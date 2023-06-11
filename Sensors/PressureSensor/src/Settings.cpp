#include <Preferences.h>
#include <string> 
#include "Settings.h"

Preferences g_preferences;


void saveMqttSettingsToPreferences(const MqttDetails &mqtt)
{
    Serial.println("Saving mqtt preferences: " + mqtt.ToString());

    g_preferences.begin("pr-mqtt", /*readonly*/ false); 

    g_preferences.putString("BUrl", mqtt.BrockerUrl.c_str());
    g_preferences.putString("BPort",String(mqtt.BrockerPort).c_str());
    g_preferences.putString("BUsername", mqtt.Username.c_str());
    g_preferences.putString("BPassword", mqtt.Password.c_str());
    g_preferences.putString("BBaseTopic", mqtt.BaseTopic.c_str());
    g_preferences.putBool("BReadAllTime", mqtt.ReadAllTheTime);

    g_preferences.end();
}

MqttDetails const  loadMqttSettingsFromPreferences()
{
    g_preferences.begin("pr-mqtt", /*readonly*/ true); 

    MqttDetails settings;

    settings.BrockerUrl = g_preferences.getString("BUrl", "");
    settings.BrockerPort = std::stoi(g_preferences.getString("BPort", "8883").c_str());
    settings.Username = g_preferences.getString("BUsername", "");
    settings.Password = g_preferences.getString("BPassword", "");
    settings.BaseTopic = g_preferences.getString("BBaseTopic", "");
    settings.ReadAllTheTime = g_preferences.getBool("BReadAllTime", false);

    g_preferences.end();

    settings.BaseTopic.trim();
    settings.BrockerUrl.trim();
    settings.Username.trim();
    
    Serial.println("Loaded mqtt preferences: " + settings.ToString());
    return settings;
}
