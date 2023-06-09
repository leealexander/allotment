#ifndef __PRESSURESENSOR_H__
#define __PRESSURESENSOR_H__

#include <Adafruit_ADS1X15.h>

class PressureSensor
{
    public:
    bool Initialise();
    int GetReading();

    private:
    Adafruit_ADS1115 _ads; 
};

#endif