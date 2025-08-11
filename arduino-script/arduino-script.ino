#include <Arduino_BuiltIn.h>

#if defined(ESP8266)
#include <ESP8266WiFi.h>
#else
#include <WiFi.h>
#endif


// Network configurations
char ssid[] = "vicon";     // your network SSID (name) // 5ghz band not supported, only 2.4ghz.
char pass[] = "infotech";  // your network password

// TCP Configuration
WiFiServer server(8888);  // TCP server on port 8888
WiFiClient client;        // TCP client connection

// Button configurations
const unsigned int BUTTON_PIN = 10;
const unsigned long DEBOUNCE_DELAY = 50;  // 50 ms

bool isUserStudyStarted = false;
bool lastButtonState = HIGH;
bool currentButtonState = HIGH;
unsigned long lastDebounceTime = 0;

// Connection management
unsigned long lastHeartbeat = 0;
const unsigned long HEARTBEAT_INTERVAL = 5000;  // 5 s
bool isClientConnected = false;

void setup() {
  Serial.begin(9600);
  delay(1000);

  // Initialize button
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  Serial.println("Button initialized on pin 10");

  // Connect to WiFi
  Serial.print("Connecting to WiFi...");
  WiFi.begin(ssid, pass);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println();
  Serial.println("WiFi connected!");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  // Start TCP server
  server.begin();
  Serial.printf("TCP server started on port 8888\n");
  Serial.println("Waiting for HoloLens connection...");
}

void loop() {
  handleClientConnection();
  processIncomingMessages();
  handleButtonPresses();
  sendHeartbeat();

  delay(5);
}

void handleClientConnection() {
  if (!client || !client.connected()) {
    client = server.available();

    if (client) {
      isClientConnected = true;

      Serial.println("HoloLens connected!");
      Serial.print("Client IP: ");
      Serial.println(client.remoteIP());
    } else {
      isClientConnected = false;
    }
  }
}

void processIncomingMessages() {
  if (client && client.connected() && client.available()) {
    String message = client.readStringUntil('\n');
    message.trim();  // Remove whitespace/newlines

    if (message.length() > 0) {
      Serial.println("Received: " + message);

      onDataReceived(message);
    }
  }
}

void handleButtonPresses() {
  // Read the button state
  bool reading = digitalRead(BUTTON_PIN);

  // Check if button state changed (for debouncing)
  if (reading != lastButtonState) {
    lastDebounceTime = millis();
  }

  // If enough time has passed since last state change
  if ((millis() - lastDebounceTime) > DEBOUNCE_DELAY) {
    // If button state has actually changed
    if (reading != currentButtonState) {
      currentButtonState = reading;

      // Button was released (went from LOW to HIGH with pull-up)
      if (currentButtonState == HIGH && lastButtonState == LOW) {
        Serial.println("Button released!");

        // Send to HoloLens only if system started and connected
        if (isUserStudyStarted && isClientConnected) {
          sendMessage("BUTTON_PRESSED");

          Serial.println("Button press sent to HoloLens");
        } else if (!isUserStudyStarted) {
          Serial.println("Button pressed - waiting for system start");
        } else if (!isClientConnected) {
          Serial.println("Button pressed - no HoloLens connection");
        }
      }
    }
  }

  lastButtonState = reading;
}

void onDataReceived(String data) {
  if (data == "START") {
    isUserStudyStarted = true;

    Serial.println("User study started! Button monitoring enabled for HoloLens.");

    sendMessage("READY");
  } else if (data == "END") {
    isUserStudyStarted = false;

    Serial.println("User study ended! Button monitoring disabled for HoloLens.");

    sendMessage("ENDED");
  } else if (data == "PONG") {
    Serial.println("Connection to Hololens is alive!")
  } else {
    Serial.println("Unknown command: " + data);
  }
}

void sendMessage(String message) {
  if (client && client.connected()) {
    client.println(message);
    client.flush();  // Ensure message is sent immediately

    Serial.println("Sent: " + message);
  } else {
    Serial.println("Cannot send message - no client connected");
  }
}

void sendHeartbeat() {
  if (isClientConnected && (millis() - lastHeartbeat > HEARTBEAT_INTERVAL)) {
    if (client && client.connected()) {
      sendMessage("PING");

      lastHeartbeat = millis();
    } else {
      Serial.println("Client disconnected");

      isClientConnected = false;
      isUserStudyStarted = false;  // Reset system when client disconnects
    }
  }
}