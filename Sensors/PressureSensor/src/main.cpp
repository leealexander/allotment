#include <FS.h> 
#include <Arduino.h>
#include <Adafruit_ADS1X15.h>
#include <WiFiManager.h>   
#include <HTTPClient.h>
#include <EasyButton.h>
#include <time.h>
#include <cmath> 
#include <Preferences.h>

#define FLASH_PIN 0

Adafruit_ADS1115 g_ads; 

// Pin connected to the ALERT/RDY signal for new sample notification.
constexpr int READY_PIN = 3;

// This is required on ESP32 to put the ISR in IRAM. Define as
// empty for other platforms. Be careful - other platforms may have
// other requirements.
#ifndef IRAM_ATTR
#define IRAM_ATTR
#endif

char g_polyServer[256] = {0};
WiFiManagerParameter g_urlParam("server", "Polly posting URL", g_polyServer, sizeof(g_polyServer) / sizeof(char));

WiFiManager g_wifiManager;
EasyButton g_resetButton(FLASH_PIN);
bool g_saveSettings = false;
Preferences g_preferences;

void saveConfigCallback ()
{
  g_saveSettings = true;
}


void configModeCallback (WiFiManager *pWiFiManager) {
  Serial.println("Entered config mode");
  Serial.println(WiFi.softAPIP());

  Serial.println(pWiFiManager->getConfigPortalSSID());
}

void reset() 
{
  Serial.println("Erasing stored WiFi credentials.");
  
  // clear WiFi creds.
  g_wifiManager.resetSettings();
   
  Serial.println("Restarting...");
  ESP.restart(); // builtin, safely restarts the ESP. 
}

void saveSettings()
{
  auto server = String(g_urlParam.getValue());
  Serial.println("Writing to settings server: " + server);
  g_preferences.putString("server", server.c_str());
}


void loadSettings()
{
  auto server = g_preferences.getString("server", "");
  Serial.println("Read from settings server: " + server);
  strcpy(g_polyServer, server.c_str());
}

void processSettings()
{
  g_preferences.begin("pr-app", !g_saveSettings); 

  if(g_saveSettings)
  {
    saveSettings();
  }

  loadSettings();

  g_preferences.end();
}



void setup() 
{
  Serial.begin(9600);

  g_wifiManager.addParameter(&g_urlParam);
  g_wifiManager.setAPCallback(configModeCallback);
  g_wifiManager.setSaveConfigCallback(saveConfigCallback);
  g_wifiManager.autoConnect("PRESSURE-SENSOR");

  processSettings();
  
  g_resetButton.onPressed(reset);
  g_resetButton.begin();

  if(strlen(g_polyServer) == 0)
  {
    Serial.println("No URL to PolyTunnel - resetting...");
    reset();
    return;
  }  
  Serial.println("Server=" + String(g_polyServer));

  Serial.println("Getting differential reading from AIN0 (P) and AIN1 (N)");
  Serial.println("ADC Range: +/- 6.144V (1 bit = 3mV/ADS1015, 0.1875mV/ADS1115)");
  g_ads.setGain(GAIN_ONE);

  if (!g_ads.begin()) 
  {
    Serial.println("Failed to initialize ADS.");
    ESP.restart(); // builtin, safely restarts the ESP. 
    return;
  }

  pinMode(READY_PIN, INPUT);

  // Start continuous conversions.
  g_ads.startADCReading(ADS1X15_REG_CONFIG_MUX_SINGLE_0, /*continuous=*/true);
  configTime(0, 3600, "pool.ntp.org");
}

#define SAMPLE_COUNT 20
int16_t g_readings[SAMPLE_COUNT];
int g_readingIndex = 0;
int g_skipCount = 0;
time_t  g_readingStartTime;
time_t  g_lastSample;

int avgReading()
{
  int sum = 0;
  for(int i = 0; i < SAMPLE_COUNT; i++)
  {
    sum += g_readings[i];
  }  
  return sum / SAMPLE_COUNT;
}


unsigned long getTime() {
  time_t now;
  struct tm timeinfo;
  if (!getLocalTime(&timeinfo)) {
    //Serial.println("Failed to obtain time");
    return(0);
  }
  time(&now);
  return now;
}

void loop() 
{
  g_resetButton.read();


  auto currentTime = getTime();
  if(g_readingIndex == 0)
  {
    g_lastSample = g_readingStartTime = currentTime;
    auto textTime = String(ctime(&g_readingStartTime));
    Serial.println("Start sample time:" + textTime);
  }
  else
  {
    if(currentTime - g_lastSample < 1)
    {
      return;
    }
    g_lastSample = currentTime;
  }

  int16_t reading = g_ads.getLastConversionResults();
  Serial.println("reading=" + String(reading));

  if(g_readingIndex > 0)
  {
    auto diff = abs(g_readings[g_readingIndex-1] - reading);

    if(diff > 10)
    {
      if(g_skipCount++ > 20)
      {
        g_skipCount = g_readingIndex = 0;
        Serial.println("Resetting sampling to start as diffs are not normalising.");
      }
      else
      {
        Serial.println("Skipping as diff max of 10 has been exceeded -  diff is " + String(diff));
      }
      return;
    }
  }
  Serial.println("Taking the reading=" + String(reading));
  g_readings[g_readingIndex] = reading;
  g_skipCount = 0;

  if(g_readingIndex++ >= SAMPLE_COUNT)
  {
    g_readingIndex = 0;


    HTTPClient http;
    bool http_begin = http.begin(g_polyServer);
    String payload = "startTime=" + String(g_readingStartTime) + "&reading=" + String(avgReading());  //Combine the name and value
    Serial.println("Sending: " + payload);
    
    http.addHeader("Content-Type", "application/x-www-form-urlencoded");
    int httpResponseCode = http.sendRequest("POST", payload);

    Serial.println("POST Status code: " + String(httpResponseCode));
  }
  
}