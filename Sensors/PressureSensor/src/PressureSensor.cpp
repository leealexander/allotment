#include "PressureSensor.h"

bool PressureSensor::Initialise()
{
    Serial.println("Initialising PressureSensor...");
    _ads.setGain(GAIN_ONE);

    if (!_ads.begin()) 
    {
        Serial.println("Failed to initialize PressureSensor.");
        ESP.restart(); // builtin, safely restarts the ESP. 
        return false;
    }

    _ads.startADCReading(ADS1X15_REG_CONFIG_MUX_DIFF_0_1, /*continuous=*/true);
    Serial.println("PressureSensor ready!");

    return true;
}

int PressureSensor::GetReading()
{
    auto reading = abs(_ads.getLastConversionResults());
    Serial.println("Reading: " + String(reading));
    return reading;
}