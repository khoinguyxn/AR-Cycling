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
const char* serverAddress = "ar-cycling.up.railway.app";
const int serverPort = 443;

// Button configurations
const unsigned int BUTTON_PIN = 10;

// Button state
bool lastButtonState = HIGH;
bool currentButtonState = HIGH;

// Wifi client
WiFiSSLClient wifiClient;

// NFP config
int wifiStatus = WL_IDLE_STATUS;
WiFiUDP Udp;  // A UDP instance to let us send and receive packets over UDP
NTPClient timeClient(Udp);
const int TIMEZONE_OFFSET = 0;

// Timing and buffering
const int MAX_BUFFER_SIZE = 12;

// Event buffer
std::vector<String> eventBuffer;

const char* ROOT_CA =
  "-----BEGIN CERTIFICATE-----\n"
  "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\n"
  "sg==\n"
  "-----END CERTIFICATE-----\n";

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
    wifiClient.setCACert(ROOT_CA);

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