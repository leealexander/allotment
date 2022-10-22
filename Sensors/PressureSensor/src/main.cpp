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

String g_polyServer;
String g_polyApiKey;
WiFiManagerParameter g_urlParam("server", "Polly posting URL", "", 256);
WiFiManagerParameter g_apiKeyParam("apiKey", "Polly posting ApiKey","", 256);

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
  g_polyServer = String(g_urlParam.getValue());
  g_polyApiKey= String(g_apiKeyParam.getValue());
  
  Serial.println("Writing to settings server: " + server);
  g_preferences.putString("server", server.c_str());
  g_preferences.putString("apiKey", apiKey.c_str());
}


void loadSettings()
{
  g_polyServer = g_preferences.getString("server", "");
  g_polyApiKey = g_preferences.getString("apiKey", "");
  Serial.println("Read from settings server: " + g_polyServer);
  Serial.println("Read from settings apiKey: " + g_polyApiKey);
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
  g_wifiManager.addParameter(&g_apiKeyParam);
  g_wifiManager.setAPCallback(configModeCallback);
  g_wifiManager.setSaveConfigCallback(saveConfigCallback);
  g_wifiManager.autoConnect("PRESSURE-SENSOR");

  processSettings();
  
  g_resetButton.onPressed(reset);
  g_resetButton.begin();

  if(g_polyServer.length() == 0)
  {
    Serial.println("No URL to PolyTunnel - resetting...");
    reset();
    return;
  }  
  Serial.println("Server=" + g_polyServer);

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

  Serial.println("Taking the reading=" + String(reading));
  g_readings[g_readingIndex] = reading;

  if(g_readingIndex++ >= SAMPLE_COUNT)
  {
    g_readingIndex = 0;

    HTTPClient http;
    http.begin(g_polyServer);
    http.addHeader("Authorization", "Bearer:" + g_polyApiKey);
    String payload = "readingTimeUtc=" + String(g_readingStartTime) + "&reading=" + String(avgReading());  //Combine the name and value
    Serial.println("Sending: " + payload);
    
    http.addHeader("Content-Type", "application/x-www-form-urlencoded");
    int httpResponseCode = http.sendRequest("POST", payload);

    Serial.println("POST Status code: " + String(httpResponseCode));
  }
}