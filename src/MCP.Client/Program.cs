using MCP.Client.Application.Services;
using MCP.Client.Domain.Entities;
using MCP.Client.Domain.Interfaces;
using MCP.Client.Infrastructure.AI;
using MCP.Client.Infrastructure.Speech;

// Obtener configuración desde variables de entorno
string? openAiKey = Environment.GetEnvironmentVariable("OpenAI-KEY");
string model = "gpt-5-nano";
if (string.IsNullOrWhiteSpace(openAiKey))
{
    Console.WriteLine("Error: La variable de entorno 'OpenAI-KEY' no está configurada.");
    return;
}

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

// Configurar servicios de infraestructura (Dependency Injection manual)
ISpeechRecognitionService speechRecognitionService = new AzureSpeechRecognitionService(
    speechKey, 
    speechRegion, 
    language: "es-AR");

ISpeechSynthesisService speechSynthesisService = new AzureSpeechSynthesisService(
    speechKey, 
    speechRegion, 
    language: "es-AR", 
    voiceName: "es-AR-TomasNeural"); //es-AR-ElenaNeural //es-AR-TomasNeural

// Obtener la ruta del proyecto del servidor MCP (opcional)
// Si no se proporciona, el agente funcionará sin herramientas MCP
string? mcpServerProjectPath = Environment.GetEnvironmentVariable("MCP_SERVER_PROJECT_PATH");
if (string.IsNullOrWhiteSpace(mcpServerProjectPath))
{
    // Intentar usar la ruta relativa al proyecto MCP.Server.API
    var currentDir = Directory.GetCurrentDirectory();
    var serverProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "MCP.Server.API", "MCP.Server.API.csproj"));
    if (File.Exists(serverProjectPath))
    {
        mcpServerProjectPath = serverProjectPath;
        Console.WriteLine($"✅ Servidor MCP encontrado en: {mcpServerProjectPath}");
    }
    else
    {
        Console.WriteLine("⚠️ Advertencia: No se encontró el proyecto del servidor MCP. El agente funcionará sin herramientas MCP.");
    }
}

IAIAgentService aiAgentService = new OpenAIAgentService(
    openAiKey, 
    model, 
    instructions: "You are a helpful and friendly assistant. Respond in a conversational and natural way in Spanish.",
    mcpServerProjectPath: mcpServerProjectPath);

// Crear servicio de aplicación
IVoiceAssistantService voiceAssistant = new VoiceAssistantService(
    speechRecognitionService,
    speechSynthesisService,
    aiAgentService);

// Configurar eventos para la UI
voiceAssistant.UserSpoke += (s, text) =>
{
    Console.WriteLine($"\n\nTú: {text}");
};

voiceAssistant.AssistantResponding += (s, userInput) =>
{
    Console.Write("Asistente pensando... ");
};

voiceAssistant.AssistantResponded += (s, response) =>
{
    Console.WriteLine($"\nAsistente: {response}");
    Console.WriteLine("\n---\nDi algo más...");
};

voiceAssistant.ErrorOccurred += (s, error) =>
{
    Console.WriteLine($"\nError: {error}");
};

// Suscribirse a eventos de reconocimiento intermedio para mostrar en tiempo real
speechRecognitionService.Recognizing += (s, result) =>
{
    if (!string.IsNullOrEmpty(result.Text))
    {
        Console.Write($"\rEscuchando: {result.Text}   ");
    }
};

speechRecognitionService.Recognized += (s, result) =>
{
    if (result.Status == RecognitionStatus.NoMatch)
    {
        Console.WriteLine("\nNo se pudo reconocer el audio. Intenta de nuevo.");
    }
};

speechRecognitionService.ErrorOccurred += (s, error) =>
{
    Console.WriteLine($"\nError de reconocimiento: {error}");
};

speechRecognitionService.SessionStopped += (s, e) =>
{
    Console.WriteLine("\nSesión detenida.");
};

// Iniciar la aplicación
Console.WriteLine("=== Cliente MCP de Voz ===");
Console.WriteLine("Di algo para comenzar la conversación...");
Console.WriteLine("(Di 'salir' o 'exit' para terminar)\n");

try
{
    await voiceAssistant.StartAsync();

    // Mantener el programa corriendo hasta que se presione Ctrl+C
    var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cancellationTokenSource.Cancel();
    };

    while (!cancellationTokenSource.Token.IsCancellationRequested)
    {
        await Task.Delay(100, cancellationTokenSource.Token);
    }

    await voiceAssistant.StopAsync();
}
catch (OperationCanceledException)
{
    // Ctrl+C presionado
    await voiceAssistant.StopAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"\nError inesperado: {ex.Message}");
    await voiceAssistant.StopAsync();
}
finally
{
    // Limpiar recursos
    if (speechRecognitionService is IDisposable speechDisposable)
        speechDisposable.Dispose();
    if (speechSynthesisService is IDisposable synthesisDisposable)
        synthesisDisposable.Dispose();
    if (aiAgentService is IDisposable aiDisposable)
        aiDisposable.Dispose();
}

Console.WriteLine("\nPrograma finalizado.");
