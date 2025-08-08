#include "Keyboard.h"

const int BUTTON_PIN = 10;
bool lastButtonState = HIGH;

void setup() {
  Serial.begin(9600);

  Keyboard.begin();

  // Keyboard.println("READY");
}

void loop() {
  // const bool currentButtonState = digitalRead(BUTTON_PIN);

  // if (lastButtonState == HIGH && currentButtonState == LOW) {
  //   Keyboard.println("BUTTON_PRESSED");
  // }

  // lastButtonState = currentButtonState;
  // delay(10);
  Keyboard.press('a');
  delay(100);
  Keyboard.release('a');
  delay(1000);
}