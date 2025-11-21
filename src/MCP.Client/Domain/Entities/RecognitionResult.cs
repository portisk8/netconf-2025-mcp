namespace MCP.Client.Domain.Entities;

public class RecognitionResult
{
    public string Text { get; set; } = string.Empty;
    public RecognitionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum RecognitionStatus
{
    Recognized,
    NoMatch,
    Error
}

