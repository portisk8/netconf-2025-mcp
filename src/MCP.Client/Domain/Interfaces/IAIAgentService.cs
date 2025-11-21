namespace MCP.Client.Domain.Interfaces;

public interface IAIAgentService
{
    Task<string> GetResponseAsync(string userInput, CancellationToken cancellationToken = default);
}

