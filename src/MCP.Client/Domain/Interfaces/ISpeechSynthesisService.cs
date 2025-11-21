namespace MCP.Client.Domain.Interfaces;

public interface ISpeechSynthesisService
{
    Task SpeakAsync(string text, CancellationToken cancellationToken = default);
    Task StopSpeakingAsync(CancellationToken cancellationToken = default);
}

