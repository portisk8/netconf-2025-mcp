#include <DHT.h>

#define DHTPIN A0     // Pin donde está conectado el sensor DHT11
#define DHTTYPE DHT11 // Definimos el tipo de sensor

DHT dht(DHTPIN, DHTTYPE);

// Definición de pines para los LEDs
const int ledPins[] = {8, 9, 10, 11};
const int numLeds = 4;

void setup() {
  Serial.begin(9600);
  dht.begin();

  // Configurar pines de LEDs como salida
  for (int i = 0; i < numLeds; i++) {
    pinMode(ledPins[i], OUTPUT);
    digitalWrite(ledPins[i], HIGH); // Inicialmente apagados (Lógica inversa)
  }
}

void loop() {
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim(); // Eliminar espacios en blanco y saltos de línea

    if (command.startsWith("led-")) {
      handleLedCommand(command);
    } else if (command == "temp") {
      float t = dht.readTemperature();
      if (isnan(t)) {
        Serial.println("Error leyendo temperatura");
      } else {
        Serial.println(t);
      }
    } else if (command == "humedad") {
      float h = dht.readHumidity();
      if (isnan(h)) {
        Serial.println("Error leyendo humedad");
      } else {
        Serial.println(h);
      }
    }
  }
}

void handleLedCommand(String command) {
  int firstDash = command.indexOf('-');
  
  // Caso consulta: led-[num]?
  if (command.endsWith("?")) {
    int questionMark = command.indexOf('?');
    String numStr = command.substring(firstDash + 1, questionMark);
    int ledNum = numStr.toInt();
    
    if (ledNum < 1 || ledNum > 4) return;
    
    int pinIndex = ledNum - 1;
    // Leer estado actual (Active Low: LOW=on, HIGH=off)
    int state = digitalRead(ledPins[pinIndex]);
    
    Serial.print("led-");
    Serial.print(ledNum);
    if (state == LOW) {
      Serial.println("-on");
    } else {
      Serial.println("-off");
    }
    return;
  }

  // Caso comando: led-[num]-[on/off]
  // Ejemplo: led-1-on
  int secondDash = command.lastIndexOf('-');
  
  if (firstDash == -1 || secondDash == -1 || firstDash == secondDash) {
    return; // Formato incorrecto
  }

  String numStr = command.substring(firstDash + 1, secondDash);
  String action = command.substring(secondDash + 1);
  
  int ledNum = numStr.toInt();
  
  if (ledNum < 1 || ledNum > 4) {
    return; // Número de LED inválido
  }

  int pinIndex = ledNum - 1;
  bool turnOn = (action == "on");

  if (turnOn) {
    digitalWrite(ledPins[pinIndex], LOW);
    Serial.print("led-");
    Serial.print(ledNum);
    Serial.println("-on");
  } else if (action == "off") {
    digitalWrite(ledPins[pinIndex], HIGH);
    Serial.print("led-");
    Serial.print(ledNum);
    Serial.println("-off");
  }
}
