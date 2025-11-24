# Documentación de Comandos Arduino IoT

Este documento describe el protocolo de comunicación serial implementado en el firmware `IoT.ino`.

**Configuración Serial:**
- **Baud Rate:** 9600
- **Terminador de línea:** `\n` (Nueva línea)

## Comandos Disponibles

### 1. Control de LEDs
Controla el estado (encendido/apagado) de los 4 LEDs conectados.

*   **Formato:** `led-[num]-[accion]`
    *   `[num]`: Número del LED (1 a 4).
    *   `[accion]`: `on` para encender, `off` para apagar.
*   **Ejemplos:**
    *   `led-1-on`: Enciende el LED 1.
    *   `led-3-off`: Apaga el LED 3.
*   **Respuesta:** Retorna el estado resultante en el mismo formato.
    *   Ejemplo: `led-1-on`

### 2. Consulta de Estado de LED
Consulta si un LED específico está encendido o apagado.

*   **Formato:** `led-[num]?`
    *   `[num]`: Número del LED (1 a 4).
*   **Ejemplo:** `led-2?`
*   **Respuesta:** `led-[num]-on` o `led-[num]-off` según el estado actual.

### 3. Temperatura
Lee la temperatura actual del sensor DHT11.

*   **Comando:** `temp`
*   **Respuesta:** Valor numérico en grados Celsius (ej: `24.50`) o mensaje de error.

### 4. Humedad
Lee la humedad relativa actual del sensor DHT11.

*   **Comando:** `humedad`
*   **Respuesta:** Valor numérico en porcentaje (ej: `60.00`) o mensaje de error.

## Notas de Hardware
- **LEDs:** Conectados a pines digitales 8, 9, 10, 11. Lógica inversa (Active Low).
- **Sensor DHT11:** Conectado a pin analógico A0.
