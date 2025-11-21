using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using MCP.Client.Domain.Interfaces;
using OpenAI;

namespace MCP.Client.Infrastructure.AI;

public class OpenAIAgentService : IAIAgentService, IDisposable
{
    private readonly AIAgent _agent;
    private bool _disposed;

    public OpenAIAgentService(string openAiKey, string model = "gpt-5-nano", string instructions = "You are a helpful and friendly assistant. Respond in a conversational and natural way in Spanish.")
    {
        var client = new OpenAIClient(openAiKey);
        _agent = client
            .GetChatClient(model)
            .CreateAIAgent(instructions: instructions, name: "VoiceAssistant");
    }

    public async Task<string> GetResponseAsync(string userInput, CancellationToken cancellationToken = default)
    {
        var response = await _agent.RunAsync(userInput);
        return response.ToString();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // El AIAgent no implementa IDisposable, pero podemos limpiar recursos si es necesario
        _disposed = true;
    }
}

