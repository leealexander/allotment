#ifndef __MQTT_H__
#define __MQTT_H__

bool InitMqtt();

bool ShouldTakeReading();
bool PostReading(int reading);

#endif
