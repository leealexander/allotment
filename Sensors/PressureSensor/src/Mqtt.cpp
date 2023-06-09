#include <string> 
#include "Mqtt.h"
#include "WifiHandler.h" 
#include "Settings.h"
#include "PubSubClient.h"
#include "TimeUtils.h"

PubSubClient g_mqttClient(g_wifiClient);
int g_readingCount = 1;
bool g_readAllTheTime = false;

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

void receiveMessageCallback(char* topic, byte* messageRaw, unsigned int length) 
{
    auto message = ConvertMessageRawToString(messageRaw, length);

    Serial.print("Message arrived on topic: " + message);

    auto key = parseKey(message);
    auto value = parseValue(message);
    if(key == "ReadCount")
    {
        auto times = std::stoi(value.c_str());
        if(times > 0)
        {
            g_readingCount = times;
        }
        else if(times == -1)
        {
            auto settings = loadMqttSettingsFromPreferences();
            settings.ReadAllTheTime = g_readAllTheTime = true;
            saveMqttSettingsToPreferences(settings);
        }
    }
}

bool InitMqtt()
{
    auto settings = loadMqttSettingsFromPreferences();
    g_mqttClient.setServer(settings.BrockerUrl.c_str(), settings.BrockerPort);    
    g_mqttClient.setCallback(receiveMessageCallback);
    g_mqttClient.subscribe( (settings.BaseTopic +  "water-pressure/command").c_str());

    if(!settings.AreValid())
    {
        Serial.println("Settings aren't valid, failing InitMqtt: " + settings.ToString());
        return false;
    }  

    return true;
}

bool ShouldTakeReading()
{
    return g_readAllTheTime || g_readingCount > 0;
}

bool CheckAndConnectMqtt()
{
  if(!g_mqttClient.connected())
  {
    auto settings = loadMqttSettingsFromPreferences();
    if (g_mqttClient.connect("PressureSensor",settings.Username.c_str(), settings.Password.c_str())) 
    {
      Serial.println("mqtt Connected");
      return true;
    } 
    else 
    {
      Serial.print("mqtt Connection failed: ");
      Serial.println(g_mqttClient.state());
    }
  }

  return false;
}


bool PostReading(int reading)
{
    auto message = "time=" + String(getTime()) + ", Pressure=" + String(reading);
    auto settings = loadMqttSettingsFromPreferences();
    auto topic = settings.BaseTopic +  "water-pressure";
    auto sent = CheckAndConnectMqtt() && g_mqttClient.publish( topic.c_str(), message.c_str(), /*retained*/ true);
    if(sent)
    {
        Serial.println("Posted value");
        g_readingCount--;
    }
    else
    {
        Serial.println("Failed to post value");
    }

    return sent;
}
