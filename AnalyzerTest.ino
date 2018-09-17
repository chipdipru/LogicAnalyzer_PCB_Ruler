
#if defined(ARDUINO) && ARDUINO >= 100
#include "Arduino.h"
#else
#include "WProgram.h"
#endif

#include "clsPCA9555.h"
#include "Wire.h"



#define   EXTRA_OUT      6


#define   PWM_PIN        6
#define   PWM_PIN_2      9

#define   LED_PIN        13 
#define   EXTRA_IN_PIN   7


PCA9555 ioport(0x20);


void setup()
{ 
  pinMode(PWM_PIN, OUTPUT); 
  pinMode(PWM_PIN_2, OUTPUT);
  analogWrite(PWM_PIN, 128);
  pinMode(LED_PIN, OUTPUT); 
  
  for (uint8_t i = 0; i < EXTRA_OUT; i++)
    ioport.pinMode(i, OUTPUT);

  ioport.pinMode(EXTRA_IN_PIN, INPUT);
}

void loop()
{  
    for (uint8_t i = 0; i < EXTRA_OUT; i++)
    {
      ioport.digitalWrite(i, HIGH);
      delayMicroseconds(5);
    }

    if (ioport.digitalRead(EXTRA_IN_PIN) == LOW)
    {
      digitalWrite(LED_PIN, HIGH);
      analogWrite(PWM_PIN_2, 192);
    }
    else
    {
      digitalWrite(LED_PIN, LOW);
      digitalWrite(PWM_PIN_2, LOW);
    }

    for (uint8_t i = 0; i < EXTRA_OUT; i++)
    {
      ioport.digitalWrite(i, LOW);
      delayMicroseconds(5);
    }
}
