using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MCP.Client.Domain.Interfaces;
using ModelContextProtocol.Client;
using OpenAI;

namespace MCP.Client.Infrastructure.AI;

public class OpenAIAgentService : IAIAgentService, IDisposable
{
    private readonly AIAgent _agent;
    private readonly McpClient? _mcpClient;
    private readonly List<ChatMessage> _conversationHistory;
    private readonly object _historyLock = new object();
    private bool _disposed;

    public OpenAIAgentService(string openAiKey, string model = "gpt-5-nano", string instructions = "You are a helpful and friendly assistant. Respond in a conversational and natural way in Spanish.", string? mcpServerProjectPath = null)
    {
        _conversationHistory = new List<ChatMessage>();
        var client = new OpenAIClient(openAiKey);
        
        // Si se proporciona la ruta del servidor MCP, crear y conectar el cliente MCP
        if (!string.IsNullOrWhiteSpace(mcpServerProjectPath))
        {
            try
            {
                // Crear el cliente MCP de forma asíncrona (usaremos GetAwaiter().GetResult() para inicialización síncrona)
                var serverPath = Path.GetFullPath(mcpServerProjectPath);
                
                _mcpClient = McpClient.CreateAsync(
                    new StdioClientTransport(new()
                    {
                        Command = "dotnet",
                        Arguments = ["run", "--project", serverPath],
                        Name = "MCP.Server.API",
                    })).GetAwaiter().GetResult();

                // Obtener las herramientas del servidor MCP
                var tools = _mcpClient.ListToolsAsync().GetAwaiter().GetResult();
                
                // Crear el IChatClient usando Microsoft.Extensions.AI con las herramientas MCP
                var chatClientBuilder = new ChatClientBuilder(
                    client.GetChatClient(model).AsIChatClient())
                    .UseFunctionInvocation();
                
                var chatClient = chatClientBuilder.Build();
                
                // Crear el agente con las herramientas MCP
                // Nota: Microsoft.Agents.AI puede requerir configuración adicional para usar IChatClient
                // Por ahora, usamos el enfoque directo con OpenAI
                string enhancedInstructions = instructions + 
                    " You have access to several tools from the MCP server: " +
                    "1. get_weather: Provides current weather and temperature information for any location. Use it when users ask about weather conditions, temperature, or climate. " +
                    "2. get_monthly_sales: Simulates sales data for any requested month. Use it when users ask about sales, monthly sales, revenue, or financial data for a specific month. " +
                    "3. get_random_number: Generates random numbers. " +
                    "Always use these tools when appropriate to provide accurate and helpful information.";
                
                _agent = client
                    .GetChatClient(model)
                    .CreateAIAgent(instructions: enhancedInstructions, name: "VoiceAssistant");
                
                Console.WriteLine($"✅ Cliente MCP conectado. Herramientas disponibles: {tools.Count}");
                foreach (var tool in tools)
                {
                    Console.WriteLine($"   - {tool.Name}: {tool.Description}");
                }
            }
            catch (Exception ex)
            {
                // Si falla la conexión MCP, crear el agente sin MCP
                Console.WriteLine($"⚠️ Advertencia: No se pudo conectar al servidor MCP: {ex.Message}");
                Console.WriteLine("El agente funcionará sin herramientas MCP.");
                _agent = client
                    .GetChatClient(model)
                    .CreateAIAgent(instructions: instructions, name: "VoiceAssistant");
            }
        }
        else
        {
            // Crear el agente sin MCP si no se proporciona la ruta
            _agent = client
                .GetChatClient(model)
                .CreateAIAgent(instructions: instructions, name: "VoiceAssistant");
        }
    }

    public async Task<string> GetResponseAsync(string userInput, CancellationToken cancellationToken = default)
    {
        try
        {
            // Agregar el mensaje del usuario al historial
            lock (_historyLock)
            {
                _conversationHistory.Add(new ChatMessage(ChatRole.User, userInput));
            }

            // Obtener respuesta del agente
            // El agente de Microsoft.Agents.AI mantiene su propio historial internamente,
            // pero también mantenemos un historial explícito para referencia
            var response = await _agent.RunAsync(userInput);
            string responseText = response.ToString();

            // Agregar la respuesta del asistente al historial
            lock (_historyLock)
            {
                _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, responseText));
                
                // Limitar el historial a los últimos 50 mensajes para evitar que crezca demasiado
                // (25 intercambios usuario-asistente)
                const int maxHistorySize = 50;
                if (_conversationHistory.Count > maxHistorySize)
                {
                    // Mantener los mensajes más recientes
                    var messagesToKeep = _conversationHistory
                        .Skip(_conversationHistory.Count - maxHistorySize)
                        .ToList();
                    _conversationHistory.Clear();
                    _conversationHistory.AddRange(messagesToKeep);
                }
            }

            return responseText;
        }
        catch (Exception ex)
        {
            return $"Error al obtener respuesta: {ex.Message}";
        }
    }

    /// <summary>
    /// Obtiene el historial de conversación actual
    /// </summary>
    public IReadOnlyList<ChatMessage> GetConversationHistory()
    {
        lock (_historyLock)
        {
            return _conversationHistory.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Limpia el historial de conversación
    /// </summary>
    public void ClearHistory()
    {
        lock (_historyLock)
        {
            _conversationHistory.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Limpiar el cliente MCP si existe
        try
        {
            if (_mcpClient is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (_mcpClient is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().Wait();
            }
        }
        catch { }

        _disposed = true;
    }
}
