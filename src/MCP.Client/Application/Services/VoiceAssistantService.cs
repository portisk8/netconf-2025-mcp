using MCP.Client.Domain.Interfaces;

namespace MCP.Client.Application.Services;

public class VoiceAssistantService : IVoiceAssistantService
{
    private readonly ISpeechRecognitionService _speechRecognitionService;
    private readonly ISpeechSynthesisService _speechSynthesisService;
    private readonly IAIAgentService _aiAgentService;
    private bool _isRunning;
    private CancellationTokenSource? _currentOperationCancellation;
    private readonly object _lockObject = new object();

    public event EventHandler<string>? UserSpoke;
    public event EventHandler<string>? AssistantResponding;
    public event EventHandler<string>? AssistantResponded;
    public event EventHandler<string>? ErrorOccurred;

    public VoiceAssistantService(
        ISpeechRecognitionService speechRecognitionService,
        ISpeechSynthesisService speechSynthesisService,
        IAIAgentService aiAgentService)
    {
        _speechRecognitionService = speechRecognitionService;
        _speechSynthesisService = speechSynthesisService;
        _aiAgentService = aiAgentService;

        // Suscribirse a eventos del servicio de reconocimiento
        _speechRecognitionService.Recognizing += OnRecognizing;
        _speechRecognitionService.Recognized += OnRecognized;
        _speechRecognitionService.ErrorOccurred += OnErrorOccurred;
        _speechRecognitionService.SessionStopped += OnSessionStopped;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            return;

        _isRunning = true;
        await _speechRecognitionService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        await _speechRecognitionService.StopAsync(cancellationToken);
    }

    private async void OnRecognizing(object? sender, Domain.Entities.RecognitionResult result)
    {
        // Cuando el usuario empieza a hablar, interrumpir cualquier operación en curso
        // Solo interrumpir si hay texto válido (no vacío ni solo espacios)
        if (IsValidText(result.Text))
        {
            await InterruptCurrentOperationAsync();
        }
    }

    private async void OnRecognized(object? sender, Domain.Entities.RecognitionResult result)
    {
        if (result.Status != Domain.Entities.RecognitionStatus.Recognized)
            return;

        string userInput = result.Text?.Trim() ?? string.Empty;

        // Validar que el texto sea válido antes de procesar
        if (!IsValidText(userInput))
        {
            // Si no hay texto válido, no hacer nada (no interrumpir)
            return;
        }

        // Cancelar cualquier operación anterior solo si hay texto válido
        await InterruptCurrentOperationAsync();

        UserSpoke?.Invoke(this, userInput);

        // Verificar si el usuario quiere salir
        if (userInput.Contains("salir", StringComparison.OrdinalIgnoreCase) ||
            userInput.Contains("exit", StringComparison.OrdinalIgnoreCase))
        {
            await StopAsync();
            string goodbyeMessage = "Hasta luego. ¡Que tengas un buen día!";
            await _speechSynthesisService.SpeakAsync(goodbyeMessage);
            AssistantResponded?.Invoke(this, goodbyeMessage);
            return;
        }

        // Crear un nuevo token de cancelación para esta operación
        CancellationTokenSource cts = new CancellationTokenSource();
        lock (_lockObject)
        {
            _currentOperationCancellation?.Cancel();
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = cts;
        }

        // Obtener respuesta del agente
        try
        {
            AssistantResponding?.Invoke(this, userInput);
            string response = await _aiAgentService.GetResponseAsync(userInput, cts.Token);
            
            // Verificar si fue cancelado antes de continuar
            if (cts.Token.IsCancellationRequested)
                return;

            AssistantResponded?.Invoke(this, response);

            // Convertir respuesta a voz (también puede ser cancelada)
            await _speechSynthesisService.SpeakAsync(response, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // La operación fue cancelada porque el usuario habló de nuevo
            // No hacer nada, el nuevo reconocimiento se procesará
            return;
        }
        catch (Exception ex)
        {
            if (cts.Token.IsCancellationRequested)
                return;

            string errorMsg = $"Lo siento, ocurrió un error: {ex.Message}";
            ErrorOccurred?.Invoke(this, errorMsg);
            await _speechSynthesisService.SpeakAsync(errorMsg, cts.Token);
        }
        finally
        {
            lock (_lockObject)
            {
                if (_currentOperationCancellation == cts)
                {
                    _currentOperationCancellation?.Dispose();
                    _currentOperationCancellation = null;
                }
            }
        }
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        ErrorOccurred?.Invoke(this, error);
    }

    private void OnSessionStopped(object? sender, EventArgs e)
    {
        _isRunning = false;
    }

    private async Task InterruptCurrentOperationAsync()
    {
        CancellationTokenSource? ctsToCancel = null;
        lock (_lockObject)
        {
            if (_currentOperationCancellation != null)
            {
                ctsToCancel = _currentOperationCancellation;
            }
        }

        if (ctsToCancel != null)
        {
            // Cancelar la operación en curso
            ctsToCancel.Cancel();
            
            // Detener la síntesis de voz inmediatamente
            try
            {
                await _speechSynthesisService.StopSpeakingAsync();
            }
            catch
            {
                // Ignorar errores al detener
            }
        }
    }

    /// <summary>
    /// Valida que el texto sea válido (no vacío ni solo espacios en blanco)
    /// </summary>
    private static bool IsValidText(string? text)
    {
        return !string.IsNullOrWhiteSpace(text);
    }
}

