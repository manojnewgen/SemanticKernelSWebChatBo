namespace SemanticKernelSWebChatBot.Models;

public class ChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
