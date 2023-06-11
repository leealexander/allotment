#include <string> 
#include "Mqtt.h"
#include "WifiHandler.h" 
#include "Settings.h"
#include "PubSubClient.h"
#include "TimeUtils.h"

PubSubClient g_mqttClient(g_wifiClient);
int g_readingCount = 1;
bool g_readAllTheTime = false;
String g_waterPressureTopic = "water-pressure";
String g_waterPressureCommandTopic = g_waterPressureTopic + "/command";

String ConvertMessageRawToString(byte* messageRaw, unsigned int length)
{
    String message;

    for (int i = 0; i < length; i++) 
    {
        message += (char)messageRaw[i];
    }

    return message;
}

String parseValue(const String& input) {
    String value;
    auto i = input.indexOf('=');

    if (i > 0) {
        value = input.substring(i + 1);
    }

    return value;
}

String parseKey(const String& input) {
    String value;
    auto i = input.indexOf('=');

    if (i > 0) 
    {
        value = input.substring(0, i);
    }


    return value;
}

void receiveMessageCallback(char* topicRaw, byte* messageRaw, unsigned int length) 
{
    auto message = ConvertMessageRawToString(messageRaw, length);
    auto topic = String(topicRaw);

    Serial.println("Message arrived on topic=" + String(topicRaw) + ", message="+ message);


    if(topic.endsWith(g_waterPressureCommandTopic))
    {
        auto key = parseKey(message);
        auto value = parseValue(message);

        if(key == "read-count")
        {
            auto times = std::stoi(value.c_str());
            if(times >= 0)
            {
                Serial.println("Command received, readings=" + String(times));
                g_readingCount = times;
            }
            else if(times == -1)
            {
                Serial.println("Command received, switching to continuous reading mode...");
                auto settings = loadMqttSettingsFromPreferences();
                settings.ReadAllTheTime = g_readAllTheTime = true;
                saveMqttSettingsToPreferences(settings);
            }
        } else if( key == "reset")
        {
            if(value == "full")
            {
                resetWifi();                
            }
            ESP.restart(); // builtin, safely restarts the ESP. 
        } else
        {
            Serial.println("Unknown command...");
        }
    }
    else
    {
            Serial.println("Unknown topic...");
    }
}

bool CheckAndConnectMqtt()
{
    while(!g_mqttClient.connected())
    {
        Serial.print("Attempting MQTT connection...");
        auto settings = loadMqttSettingsFromPreferences();
        if (g_mqttClient.connect("PressureSensor",settings.Username.c_str(), settings.Password.c_str())) 
        {
            Serial.println("mqtt Connected");

            auto subCommand = settings.BaseTopic + g_waterPressureCommandTopic;
            if(g_mqttClient.subscribe( (subCommand).c_str()))
            {
                Serial.println("Subscribed to topic: " + subCommand);
            }
            else
            {
                Serial.println("Failed to Subscribe to topic: " + subCommand);
            }
            return true;
        } 
        else 
        {
            Serial.print("mqtt Connection failed: ");
            Serial.println(g_mqttClient.state());
            Serial.println("retying in 5 secs...");
            delay(5000);
        }
    }

  return g_mqttClient.connected();
}

void ProcessSubscriptions()
{
    CheckAndConnectMqtt();  
    g_mqttClient.loop();
}


bool InitMqtt()
{
    auto settings = loadMqttSettingsFromPreferences();
    g_mqttClient.setServer(settings.BrockerUrl.c_str(), settings.BrockerPort);    
    g_mqttClient.setCallback(receiveMessageCallback);

    g_readAllTheTime = settings.ReadAllTheTime;

    if(!settings.AreValid())
    {
        Serial.println("Settings aren't valid, failing InitMqtt: " + settings.ToString());
        return false;
    }

    CheckAndConnectMqtt();  

    return true;
}

bool ShouldTakeReading()
{
    CheckAndConnectMqtt();  
    return g_readAllTheTime || g_readingCount > 0;
}


bool PostReading(int reading)
{
    Serial.println("Posting value...");
    auto message = "time=" + String(getTime()) + ", Pressure=" + String(reading);
    auto settings = loadMqttSettingsFromPreferences();
    auto topic = settings.BaseTopic +  g_waterPressureTopic;
    auto sent = CheckAndConnectMqtt() && g_mqttClient.publish( topic.c_str(), message.c_str(), /*retained*/ true);
    if(sent)
    {
        Serial.println("Posted value");
        g_readingCount--;
    }
    else
    {
        Serial.println("Failed to post value");
       ESP.restart();
    }

    return sent;
}
