#ifndef __WIFIHANDLER_H__
#define __WIFIHANDLER_H__

#include <WiFiManager.h>   
#include <WiFiClientSecure.h>

extern WiFiClientSecure g_wifiClient;

void initialiseWifi();

void resetWifi();


#endif