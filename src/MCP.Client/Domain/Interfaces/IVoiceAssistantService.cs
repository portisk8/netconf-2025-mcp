namespace MCP.Client.Domain.Interfaces;

public interface IVoiceAssistantService
{
    event EventHandler<string>? UserSpoke;
    event EventHandler<string>? AssistantResponding;
    event EventHandler<string>? AssistantResponded;
    event EventHandler<string>? ErrorOccurred;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

