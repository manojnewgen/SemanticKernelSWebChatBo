using System.Collections.Concurrent;
using SemanticKernelSWebChatBot.Models;

namespace SemanticKernelSWebChatBot.Services;

public class ChatHistoryService
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessions = new();

    public void AddMessage(string sessionId, string sender, string message)
    {
        var messages = _sessions.GetOrAdd(sessionId, _ => new List<ChatMessage>());
        lock (messages)
        {
            messages.Add(new ChatMessage
            {
                Sender = sender,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public List<ChatMessage> GetHistory(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var messages))
        {
            lock (messages)
            {
                return messages.ToList();
            }
        }
        return new List<ChatMessage>();
    }

    public void ClearHistory(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}
