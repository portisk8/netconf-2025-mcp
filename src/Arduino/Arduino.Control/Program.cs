using System.IO.Ports;

string portName = "COM3";
int baudRate = 9600;

Console.WriteLine($"Conectando a Arduino en {portName}...");

try
{
    using SerialPort serialPort = new SerialPort(portName, baudRate);
    serialPort.Open();
    serialPort.ReadTimeout = 2000; // Timeout de 2 segundos para lectura

    Console.WriteLine("Conexión establecida.");
    Console.WriteLine("Escribe comandos (ej: led-1-on, temp, humedad). 'exit' para salir.");

    // Hilo para leer respuestas del Arduino
    Thread readThread = new Thread(() =>
    {
        while (serialPort.IsOpen)
        {
            try
            {
                string message = serialPort.ReadLine();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine($"Arduino: {message.Trim()}");
                }
            }
            catch (TimeoutException) { }
            catch (Exception ex)
            {
                // Ignorar errores al cerrar el puerto
                if (serialPort.IsOpen) Console.WriteLine($"Error lectura: {ex.Message}");
            }
        }
    });
    readThread.Start();

    while (true)
    {
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input)) continue;
        if (input.Trim().ToLower() == "exit") break;

        serialPort.WriteLine(input);
    }

    serialPort.Close();
    Console.WriteLine("Puerto cerrado.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("Asegúrate de que el Arduino esté conectado en COM3 y no esté siendo usado por otra aplicación (como el Monitor Serial).");
}
