using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using MCP.Client.Domain.Entities;
using MCP.Client.Domain.Interfaces;
using DomainRecognitionResult = MCP.Client.Domain.Entities.RecognitionResult;

namespace MCP.Client.Infrastructure.Speech;

public class AzureSpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private readonly SpeechConfig _speechConfig;
    private readonly AudioConfig _audioConfig;
    private SpeechRecognizer? _speechRecognizer;
    private bool _disposed;

    public event EventHandler<DomainRecognitionResult>? Recognizing;
    public event EventHandler<DomainRecognitionResult>? Recognized;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? SessionStopped;

    public AzureSpeechRecognitionService(string speechKey, string speechRegion, string language = "es-ES")
    {
        _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        _speechConfig.SpeechRecognitionLanguage = language;
        _audioConfig = AudioConfig.FromDefaultMicrophoneInput();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_speechRecognizer != null)
            return;

        _speechRecognizer = new SpeechRecognizer(_speechConfig, _audioConfig);

        _speechRecognizer.Recognizing += (s, e) =>
        {
            // Solo procesar si hay texto válido (no vacío ni solo espacios)
            if (!string.IsNullOrWhiteSpace(e.Result.Text))
            {
                var result = new DomainRecognitionResult
                {
                    Text = e.Result.Text,
                    Status = RecognitionStatus.Recognized
                };
                Recognizing?.Invoke(this, result);
            }
        };

        _speechRecognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                // Solo procesar si hay texto válido (no vacío ni solo espacios)
                if (!string.IsNullOrWhiteSpace(e.Result.Text))
                {
                    var result = new DomainRecognitionResult
                    {
                        Text = e.Result.Text,
                        Status = RecognitionStatus.Recognized
                    };
                    Recognized?.Invoke(this, result);
                }
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                var result = new DomainRecognitionResult
                {
                    Status = RecognitionStatus.NoMatch,
                    ErrorMessage = "No se pudo reconocer el audio"
                };
                Recognized?.Invoke(this, result);
            }
        };

        _speechRecognizer.Canceled += (s, e) =>
        {
            if (e.Reason == CancellationReason.Error)
            {
                ErrorOccurred?.Invoke(this, e.ErrorDetails);
            }
        };

        _speechRecognizer.SessionStopped += (s, e) =>
        {
            SessionStopped?.Invoke(this, EventArgs.Empty);
        };

        await _speechRecognizer.StartContinuousRecognitionAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_speechRecognizer != null)
        {
            await _speechRecognizer.StopContinuousRecognitionAsync();
            _speechRecognizer.Dispose();
            _speechRecognizer = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopAsync().GetAwaiter().GetResult();
        _speechRecognizer?.Dispose();
        _audioConfig?.Dispose();
        _disposed = true;
    }
}

