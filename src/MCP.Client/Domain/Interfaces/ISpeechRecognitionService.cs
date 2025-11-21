using MCP.Client.Domain.Entities;

namespace MCP.Client.Domain.Interfaces;

public interface ISpeechRecognitionService
{
    event EventHandler<RecognitionResult>? Recognizing;
    event EventHandler<RecognitionResult>? Recognized;
    event EventHandler<string>? ErrorOccurred;
    event EventHandler? SessionStopped;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

