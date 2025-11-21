using Microsoft.CognitiveServices.Speech;
using MCP.Client.Domain.Interfaces;

namespace MCP.Client.Infrastructure.Speech;

public class AzureSpeechSynthesisService : ISpeechSynthesisService, IDisposable
{
    private readonly SpeechSynthesizer _synthesizer;
    private bool _disposed;

    public AzureSpeechSynthesisService(string speechKey, string speechRegion, string language = "es-ES", string voiceName = "es-ES-ElviraNeural")
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechSynthesisLanguage = language;
        speechConfig.SpeechSynthesisVoiceName = voiceName;
        _synthesizer = new SpeechSynthesizer(speechConfig);
    }

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            // Si se cancela, detener la síntesis antes de lanzar la excepción
            if (cancellationToken.IsCancellationRequested)
            {
                await StopSpeakingAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Registrar un callback para detener cuando se cancele (sin await para evitar deadlock)
            using (cancellationToken.Register(() => 
            {
                // Ejecutar de forma fire-and-forget para no bloquear
                _ = Task.Run(async () => await StopSpeakingAsync());
            }))
            {
                await _synthesizer.SpeakTextAsync(text);
            }
        }
        catch (OperationCanceledException)
        {
            // La operación fue cancelada
            await StopSpeakingAsync();
            throw;
        }
    }

    public async Task StopSpeakingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // El SDK de Azure Speech tiene un método StopSpeakingAsync
            // Si no está disponible en esta versión, el try-catch lo manejará
            await _synthesizer.StopSpeakingAsync();
        }
        catch
        {
            // Ignorar errores al detener - la síntesis puede ya estar detenida
            // o el método puede no estar disponible en esta versión del SDK
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _synthesizer?.Dispose();
        _disposed = true;
    }
}

