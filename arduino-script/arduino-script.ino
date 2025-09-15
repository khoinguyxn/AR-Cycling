#include <WiFi.h>
#include <WiFiUdp.h>
#include <HttpClient.h>
#include <vector>
#include <time.h>
#include <TimeLib.h>


// Network configurations
char ssid[] = "ORBI80";          // your network SSID (name) // 5ghz band not supported, only 2.4ghz.
char pass[] = "classychair864";  // your network password

// Server configuration
const char* serverUrl = "http://127.0.0.1:8000";

// Button configurations
const unsigned int BUTTON_PIN = 10;

// Timing and buffering
unsigned long lastButtonPress = 0;
const unsigned long DEBOUNCE_DELAY = 200;
const int MAX_BUFFER_SIZE = 50;
const unsigned long SEND_INTERVAL = 30000;  // Send every 30 seconds
unsigned long lastSendTime = 0;

// Event buffer
std::vector<String> eventBuffer;

// Button state
bool lastButtonState = HIGH;
bool currentButtonState = HIGH;

// Wifi client
WiFiClient wifiClient;

// NFP config
WiFiUDP wifiUdp;
static const char ntpServerName[] = "time-a.timefreq.bldrdoc.gov";
unsigned const localNfpPort = 8080;

void setup() {
  Serial.begin(9600);
  delay(1000);

  // Initialize button
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  Serial.println("Button initialized on pin 10");

  connectToWiFi();

  wifiUdp.begin(localNfpPort);
  Serial.println("waiting for sync");
  setSyncProvider(getNtpTime);

  Serial.println("\nNTP time synchronized!");
  Serial.println("Current UTC time: " + now());

  Serial.println("Button press logger ready!");
}

void loop() {
  handleButtonPresses();

  delay(10);
}

void connectToWiFi() {
  WiFi.begin(ssid, pass);
  Serial.print("Connecting to WiFi");

  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.print(".");
  }

  Serial.println();
  Serial.println("WiFi connected!");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());
}

void handleButtonPresses() {
  // Read the button state
  bool currentButtonState = digitalRead(BUTTON_PIN);

  // Button was released (went from LOW to HIGH with pull-up)
  if (currentButtonState == HIGH && lastButtonState == LOW) {
    unsigned long currentTime = millis();

    if (currentTime - lastButtonPress > DEBOUNCE_DELAY) {
      logButtonPress();
      lastButtonPress = currentTime;
    }
  }

  lastButtonState = currentButtonState;
}

void logButtonPress() {
  time_t utcTimestamp = now();

  // Create CSV row: timestamp
  String csvRow = utcTimestamp + ",";

  eventBuffer.push_back(csvRow);

  Serial.println("Button press logged: " + utcTimestamp);
  Serial.println("Buffer size: " + String(eventBuffer.size()));

  // Force export if buffer is full
  if (eventBuffer.size() >= MAX_BUFFER_SIZE) {
    Serial.println("Buffer full, exporting immediately...");
    exportBufferedEvents();
  }
}

void exportBufferedEvents() {
  if (eventBuffer.empty()) {
    return;
  }

  Serial.println("Exporting " + String(eventBuffer.size()) + " events...");

  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("WiFi disconnected, attempting reconnect...");
    connectToWiFi();
  }

  HttpClient http(wifiClient);
  http.sendHeader("Content-Type", "application/multipart/form-data");
  http.setTimeout(10000);

  // Create batch payload
  String headers = "utc_timestamp,";
  String csvData = "";

  for (const String& event : eventBuffer) {
    csvData += event + "\\n";
  }

  String postData = "data=" + csvData + "&headers=" + headers;

  int httpResponseCode = http.post(serverUrl, postData.c_str());

  if (httpResponseCode == 200) {
    Serial.println("✓ Events exported successfully!");
    eventBuffer.clear();
    lastSendTime = millis();

  } else {
    Serial.println("Export failed: " + String(httpResponseCode));
  }

  http.stop();
}

const int NTP_PACKET_SIZE = 48;      // NTP time is in the first 48 bytes of message
byte packetBuffer[NTP_PACKET_SIZE];  //buffer to hold incoming & outgoing packets

// Taken from: https://github.com/PaulStoffregen/Time/blob/master/examples/TimeNTP_ESP8266WiFi/TimeNTP_ESP8266WiFi.ino
time_t getNtpTime() {
  IPAddress ntpServerIP;  // NTP server's ip address

  while (wifiUdp.parsePacket() > 0)
    ;  // discard any previously received packets

  Serial.println("Transmit NTP Request");

  WiFi.hostByName(ntpServerName, ntpServerIP);

  Serial.print(ntpServerName);
  Serial.print(": ");
  Serial.println(ntpServerIP);

  sendNTPpacket(ntpServerIP);

  uint32_t beginWait = millis();

  while (millis() - beginWait < 1500) {
    int size = wifiUdp.parsePacket();

    if (size >= NTP_PACKET_SIZE) {
      Serial.println("Receive NTP Response");

      wifiUdp.read(packetBuffer, NTP_PACKET_SIZE);  // read packet into the buffer

      unsigned long secsSince1900;
      // convert four bytes starting at location 40 to a long integer
      secsSince1900 = (unsigned long)packetBuffer[40] << 24;
      secsSince1900 |= (unsigned long)packetBuffer[41] << 16;
      secsSince1900 |= (unsigned long)packetBuffer[42] << 8;
      secsSince1900 |= (unsigned long)packetBuffer[43];

      return secsSince1900 - 2208988800UL + SECS_PER_HOUR;
    }
  }
  
  Serial.println("No NTP Response :-(");
  return 0;  // return 0 if unable to get the time
}

// send an NTP request to the time server at the given address
void sendNTPpacket(IPAddress& address) {
  // set all bytes in the buffer to 0
  memset(packetBuffer, 0, NTP_PACKET_SIZE);

  // Initialize values needed to form NTP request
  // (see URL above for details on the packets)
  packetBuffer[0] = 0b11100011;  // LI, Version, Mode
  packetBuffer[1] = 0;           // Stratum, or type of clock
  packetBuffer[2] = 6;           // Polling Interval
  packetBuffer[3] = 0xEC;        // Peer Clock Precision

  // 8 bytes of zero for Root Delay & Root Dispersion
  packetBuffer[12] = 49;
  packetBuffer[13] = 0x4E;
  packetBuffer[14] = 49;
  packetBuffer[15] = 52;

  // all NTP fields have been given values, now
  // you can send a packet requesting a timestamp:
  wifiUdp.beginPacket(address, 123);  //NTP requests are to port 123
  wifiUdp.write(packetBuffer, NTP_PACKET_SIZE);
  wifiUdp.endPacket();
}
