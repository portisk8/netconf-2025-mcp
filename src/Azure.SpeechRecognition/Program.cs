using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
//https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-recognize-speech?pivots=programming-language-csharp
// Obtener la clave y región de Azure Speech Services desde variables de entorno
string? speechKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
string? speechRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "eastus2";

if (string.IsNullOrWhiteSpace(speechKey))
{
    Console.WriteLine("Error: La variable de entorno 'AZURE_SPEECH_KEY' no está configurada.");
    Console.WriteLine("Para usar reconocimiento de voz, necesitas:");
    Console.WriteLine("1. Crear un recurso de Azure Speech Services en https://portal.azure.com");
    Console.WriteLine("2. Configurar la variable de entorno AZURE_SPEECH_KEY con tu clave");
    Console.WriteLine("3. (Opcional) Configurar AZURE_SPEECH_REGION (por defecto: eastus2)");
    return;
}

// Crear la configuración de Speech
var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
speechConfig.SpeechRecognitionLanguage = "es-ES"; // Español

Console.WriteLine("=== Reconocimiento de Voz en Tiempo Real ===");
Console.WriteLine("Habla en tu micrófono. Lo que digas aparecerá en la consola.");
Console.WriteLine("Presiona Ctrl+C para salir.\n");

// Configurar el audio desde el micrófono por defecto
using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

bool isRunning = true;

// Evento para resultados intermedios (mientras hablas)
speechRecognizer.Recognizing += (s, e) =>
{
    if (!string.IsNullOrEmpty(e.Result.Text))
    {
        // Imprimir en la misma línea (sobrescribiendo)
        Console.Write($"\r[Reconociendo...] {e.Result.Text}   ");
    }
};

// Evento para resultados finales (cuando terminas de hablar)
speechRecognizer.Recognized += (s, e) =>
{
    if (e.Result.Reason == ResultReason.RecognizedSpeech)
    {
        // Limpiar la línea anterior y mostrar el resultado final
        Console.WriteLine($"\r[Reconocido] {e.Result.Text}                    ");
    }
    else if (e.Result.Reason == ResultReason.NoMatch)
    {
        Console.WriteLine("\r[No reconocido] No se pudo reconocer el audio. Intenta de nuevo.");
    }
};

// Manejar cancelaciones y errores
speechRecognizer.Canceled += (s, e) =>
{
    Console.WriteLine($"\n[Cancelado] Razón: {e.Reason}");
    
    if (e.Reason == CancellationReason.Error)
    {
        Console.WriteLine($"Error Code: {e.ErrorCode}");
        Console.WriteLine($"Error Details: {e.ErrorDetails}");
        Console.WriteLine("¿Configuraste correctamente la clave y región de Azure Speech?");
    }
    
    isRunning = false;
};

// Evento cuando la sesión se detiene
speechRecognizer.SessionStopped += (s, e) =>
{
    Console.WriteLine("\n[Sesión detenida]");
    isRunning = false;
};

// Iniciar el reconocimiento continuo
Console.WriteLine("Iniciando reconocimiento de voz...\n");
await speechRecognizer.StartContinuousRecognitionAsync();

// Mantener el programa corriendo hasta que se detenga
try
{
    while (isRunning)
    {
        await Task.Delay(100);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nError inesperado: {ex.Message}");
}
finally
{
    // Detener el reconocimiento
    await speechRecognizer.StopContinuousRecognitionAsync();
    Console.WriteLine("\nPrograma finalizado.");
}
