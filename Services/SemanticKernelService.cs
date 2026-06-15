using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;

namespace SemanticKernelSWebChatBot.Services
{
    public class SemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;
        private readonly ChatHistoryService _chatHistoryService;

        public SemanticKernelService(IConfiguration configuration, ChatHistoryService chatHistoryService)
        {
            _configuration = configuration;
            _chatHistoryService = chatHistoryService;

            // Read configuration with fallback to environment variables
            string? apiKey = _configuration["OpenAI:ApiKey"] ??
                              Environment.GetEnvironmentVariable("OpenAI__ApiKey");
            string? endpoint = _configuration["OpenAI:Endpoint"] ??
                               Environment.GetEnvironmentVariable("OpenAI__Endpoint");
            string? deploymentName = _configuration["OpenAI:Model"] ??
                                     Environment.GetEnvironmentVariable("OpenAI__Model");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured. Set 'OpenAI:ApiKey' in appsettings or the environment variable 'OpenAI__ApiKey'.");
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("OpenAI endpoint is not configured. Set 'OpenAI:Endpoint' in appsettings or the environment variable 'OpenAI__Endpoint'.");
            }

            if (string.IsNullOrWhiteSpace(deploymentName))
            {
                throw new InvalidOperationException("OpenAI model/deployment name is not configured. Set 'OpenAI:Model' in appsettings or the environment variable 'OpenAI__Model'.");
            }

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey
            );

            _kernel = builder.Build();
        }

        public async Task<string> ProcessUserInputAsync(string sessionId, string userInput)
        {
            // Sanitize helper: remove control chars and collapse whitespace
            static string Sanitize(string s)
            {
                if (string.IsNullOrEmpty(s)) return string.Empty;
                // Remove non-printable control characters
                var cleaned = System.Text.RegularExpressions.Regex.Replace(s, "[\u0000-\u001F\u007F]+", " ");
                // Collapse multiple whitespace
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "\\s+", " ").Trim();
                return cleaned;
            }

            // Build a concise conversational context from the last N messages to reduce prompt size
            var history = _chatHistoryService.GetHistory(sessionId) ?? new List<Models.ChatMessage>();
            const int maxHistoryMessages = 8;
            var recent = history.Skip(Math.Max(0, history.Count - maxHistoryMessages)).ToList();

            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine("The following is a conversation between a user and an assistant. Answer concisely.");

            foreach (var msg in recent)
            {
                var text = Sanitize(msg.Message);
                if (string.Equals(msg.Sender, "User", StringComparison.OrdinalIgnoreCase))
                {
                    promptBuilder.AppendLine($"User: {text}");
                }
                else
                {
                    promptBuilder.AppendLine($"Assistant: {text}");
                }
            }

            var safeInput = Sanitize(userInput);
            promptBuilder.AppendLine($"User: {safeInput}");
            promptBuilder.AppendLine("Assistant:");

            // Limit prompt length to avoid triggering content checks due to overly long prompts
            var prompt = promptBuilder.ToString();
            const int maxPromptLength = 3000; // characters
            if (prompt.Length > maxPromptLength)
            {
                prompt = prompt.Substring(prompt.Length - maxPromptLength);
                // Ensure we start at a sensible boundary
                var firstNewLine = prompt.IndexOf('\n');
                if (firstNewLine > 0) prompt = prompt.Substring(firstNewLine + 1);
            }

            var result = await _kernel.InvokePromptAsync(prompt);
            return result.ToString();
        }
    }
}