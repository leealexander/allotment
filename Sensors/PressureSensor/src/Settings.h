#ifndef __PSSETTINGS_H__
#define __PSSETTINGS_H__
#include <WString.h>

struct MqttDetails
{
    String BrockerUrl;
    int BrockerPort;
    String Username;
    String Password;
    String BaseTopic;
    bool ReadAllTheTime;

    inline bool AreValid() const 
    {
        return BrockerUrl.length() > 0 
        && Username.length() > 0 
        && BrockerUrl.length() > 0;
    }

    inline String ToString() const 
    {
        return "MQTT: Url:" + BrockerUrl + ", Port: " + BrockerPort + ", Username: " + Username + ", Password:" +  Password;
    }
};

extern void saveMqttSettingsToPreferences(const MqttDetails &settings);
extern MqttDetails const loadMqttSettingsFromPreferences();

#endif