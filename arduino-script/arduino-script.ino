#include "Keyboard.h"

const int BUTTON_PIN = 10;
bool lastButtonState = HIGH;

void setup() {
  Serial.begin(9600);

  pinMode(BUTTON_PIN, INPUT_PULLDOWN);

  Keyboard.begin();

  Keyboard.println("READY");
}

void loop() {
  const bool currentButtonState = digitalRead(BUTTON_PIN);

  if (lastButtonState == HIGH && currentButtonState == LOW) {
    Keyboard.println("BUTTON_PRESSED");
  }

  lastButtonState = currentButtonState;
  delay(10);
}