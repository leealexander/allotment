#include <FS.h> 
#include <Arduino.h>
#include <EasyButton.h>
#include <time.h>
#include <Mqtt.h>
#include <vector>
#include "Settings.h"
#include "PressureSensor.h"
#include "IntVectorHelpers.h"
#include "WifiHandler.h"
#include "TimeUtils.h"

#define FLASH_PIN 0

PressureSensor g_pressureSensor;
std::vector<int> g_readings;


// Pin connected to the ALERT/RDY signal for new sample notification.
constexpr int READY_PIN = 3;

// This is required on ESP32 to put the ISR in IRAM. Define as
// empty for other platforms. Be careful - other platforms may have
// other requirements.
#ifndef IRAM_ATTR
#define IRAM_ATTR
#endif

EasyButton g_resetButton(FLASH_PIN);


void reset() 
{
  Serial.println("Erasing stored WiFi credentials.");
  
  // clear WiFi creds.
  resetWifi();
   
  Serial.println("Restarting...");
  ESP.restart(); // builtin, safely restarts the ESP. 
}




void setup() 
{
  Serial.begin(9600);
  
  g_resetButton.onPressed(reset);
  g_resetButton.begin();

  initialiseWifi();
  
  if(!InitMqtt())
  {
    Serial.println("InitMqtt failed,  resetting");
    reset();
  }

  if(!g_pressureSensor.Initialise())
  {
    return;
  }

  pinMode(READY_PIN, INPUT);

  configTime(0, 3600, "pool.ntp.org");
}



unsigned long g_lastSample;
bool ShouldCycle()
{
  auto current = millis();
  if(current - g_lastSample < 250)
  {
    return true;
  }
  g_lastSample = current;

  return false;
}

void loop() 
{
  g_resetButton.read();

  if(ShouldCycle() && !ShouldTakeReading())
  {
    return;
  }
  
  const int maxReadings = 20;
  const int minReadingsCount = 12;

  g_readings.push_back(g_pressureSensor.GetReading());
  if(g_readings.size() == maxReadings)
  {
    auto filteredResults = removeNoiseValues(g_readings, 1.0);
    if(filteredResults.size() >= minReadingsCount)
    {
      auto average = calculateAverage(filteredResults);
      Serial.println("Avg Reading=" + String(average));
      PostReading(average);
    }
    g_readings.clear();
  }
}
