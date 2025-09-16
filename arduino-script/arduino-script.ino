/**
 * RTC_NTPSync
 * 
 * This example shows how to set the RTC (Real Time Clock) on the Portenta C33 / UNO R4 WiFi
 * to the current date and time retrieved from an NTP server on the Internet (pool.ntp.org).
 * Then the current time from the RTC is printed to the Serial port.
 * 
 * Instructions:
 * 1. Download the NTPClient library (https://github.com/arduino-libraries/NTPClient) through the Library Manager
 * 2. Change the WiFi credentials in the arduino_secrets.h file to match your WiFi network.
 * 3. Upload this sketch to Portenta C33 / UNO R4 WiFi.
 * 4. Open the Serial Monitor.
 * 
 * Initial author: Sebastian Romero @sebromero
 * 
 * Find the full UNO R4 WiFi RTC documentation here:
 * https://docs.arduino.cc/tutorials/uno-r4-wifi/rtc
 */

// Include the RTC library
#include "RTC.h"

//Include the NTP library
#include <NTPClient.h>

#if defined(ARDUINO_PORTENTA_C33)
#include <WiFiC3.h>
#elif defined(ARDUINO_UNOWIFIR4)
#include <WiFiS3.h>
#endif

#include <WiFiUdp.h>

#include <HttpClient.h>

// Network configurations
char ssid[] = "Wi-Fi 1BCC4F 2.4G";  // your network SSID (name)
char pass[] = "Uz2e9u7z";           // your network password (use for WPA, or use as key for WEP)

// Server configuration
const char* serverAddress = "172.26.192.1";
const int serverPort = 8000;

// Button configurations
const unsigned int BUTTON_PIN = 10;

// Button state
bool lastButtonState = HIGH;
bool currentButtonState = HIGH;

// Wifi client
WiFiClient wifiClient;

// NFP config
int wifiStatus = WL_IDLE_STATUS;
WiFiUDP Udp;  // A UDP instance to let us send and receive packets over UDP
NTPClient timeClient(Udp);
const int TIMEZONE_OFFSET = 0;

// Timing and buffering
const int MAX_BUFFER_SIZE = 12;

// Event buffer
std::vector<String> eventBuffer;

void printWifiStatus() {
  // print the SSID of the network you're attached to:
  Serial.print("SSID: ");
  Serial.println(WiFi.SSID());

  // print your board's IP address:
  IPAddress ip = WiFi.localIP();
  Serial.print("IP Address: ");
  Serial.println(ip);

  // print the received signal strength:
  long rssi = WiFi.RSSI();
  Serial.print("signal strength (RSSI):");
  Serial.print(rssi);
  Serial.println(" dBm");
}

void connectToWiFi() {
  // check for the WiFi module:
  if (WiFi.status() == WL_NO_MODULE) {
    Serial.println("Communication with WiFi module failed!");
    // don't continue
    while (true)
      ;
  }

  // attempt to connect to WiFi network:
  while (wifiStatus != WL_CONNECTED) {
    Serial.print("Attempting to connect to SSID: ");
    Serial.println(ssid);
    // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
    wifiStatus = WiFi.begin(ssid, pass);

    // wait 5 seconds for connection:
    delay(5000);
  }

  Serial.println("Connected to WiFi");
  printWifiStatus();
}

void setup() {
  Serial.begin(9600);
  while (!Serial)
    ;

  connectToWiFi();
  RTC.begin();
  Serial.println("\nStarting connection to server...");
  timeClient.begin();
  timeClient.update();

  auto unixTime = timeClient.getEpochTime() + (TIMEZONE_OFFSET * 3600);

  Serial.print("Unix time = ");
  Serial.println(unixTime);

  RTCTime timeToSet = RTCTime(unixTime);
  RTC.setTime(timeToSet);

  // Retrieve the date and time from the RTC and print them
  RTCTime currentTime;
  RTC.getTime(currentTime);
  Serial.println("The RTC was just set to: " + String(currentTime));

  pinMode(BUTTON_PIN, INPUT_PULLUP);

  Serial.println("Button press logger ready!");
}

void loop() {
  handleButtonPresses();

  delay(10);
}

void handleButtonPresses() {
  // Read the button state
  bool currentButtonState = digitalRead(BUTTON_PIN);

  // Button was released (went from LOW to HIGH with pull-up)
  if (currentButtonState == HIGH && lastButtonState == LOW) {
    logButtonPress();
  }

  lastButtonState = currentButtonState;
}

void logButtonPress() {
  RTCTime currentTime;
  RTC.getTime(currentTime);

  // Create CSV row: timestamp
  String csvRow = String(currentTime) + ",";

  eventBuffer.push_back(csvRow);

  Serial.print("Button press logged: ");
  Serial.println(String(currentTime));

  Serial.print("Buffer size: ");
  Serial.println(String(eventBuffer.size()));

  // Force export if buffer is full
  if (eventBuffer.size() >= MAX_BUFFER_SIZE) {
    Serial.println("Buffer full, exporting immediately...");
    exportBuffer();
  }
}

void exportBuffer() {
  if (eventBuffer.empty()) {
    return;
  }

  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("WiFi disconnected, attempting reconnect...");
    connectToWiFi();
  }

  Serial.println("Exporting " + String(eventBuffer.size()) + " events...");

  HttpClient httpClient = HttpClient(wifiClient, serverAddress, serverPort);

  const String CONTENT_TYPE = "application/x-www-form-urlencoded";

  // Create batch payload
  String headers = "utc_timestamp,";
  String csvData = "";

  for (const String& event : eventBuffer) {
    csvData += event + "\\n";
  }

  String payload = "data=" + csvData + "&headers=" + headers;

  httpClient.post("/export", CONTENT_TYPE, payload);

  int responseStatusCode = httpClient.responseStatusCode();
  String response = httpClient.responseBody();

  Serial.print("Status code: ");
  Serial.println(responseStatusCode);

  Serial.print("Response body: ");
  Serial.println(response);

  if (responseStatusCode == 200) {
    Serial.println("Export events successfully!");
    eventBuffer.clear();
  } else {
    Serial.println("Export events failed!");
  }
}