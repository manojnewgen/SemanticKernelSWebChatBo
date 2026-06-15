using Microsoft.AspNetCore.Mvc;
using SemanticKernelSWebChatBot.Models;
using Microsoft.SemanticKernel;
using SemanticKernelSWebChatBot.Services;

namespace SemanticKernelSWebChatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly SemanticKernelService _semanticKernelService;
        private readonly ChatHistoryService _chatHistoryService;

        public ChatController(ILogger<ChatController> logger, IWebHostEnvironment env, SemanticKernelService semanticKernelService, ChatHistoryService chatHistoryService)
        {
            _logger = logger;
            _env = env;
            _semanticKernelService = semanticKernelService;
            _chatHistoryService = chatHistoryService;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("Test endpoint is working.");
        }

        [HttpGet("{sessionId}")]
        public IActionResult GetHistory(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("Session ID is required.");
            }

            var history = _chatHistoryService.GetHistory(sessionId);
            return Ok(history);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessUserInput([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserMessage))
            {
                return BadRequest("Input cannot be empty.");
            }

            // Use provided sessionId or create a new one
            var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString() : request.SessionId;

            try
            {
                _logger.LogInformation("Processing chat request: {Message}", request.UserMessage);
                string response = await _semanticKernelService.ProcessUserInputAsync(sessionId, request.UserMessage);

                _chatHistoryService.AddMessage(sessionId, "User", request.UserMessage);
                _chatHistoryService.AddMessage(sessionId, "Bot", response);

                return Ok(new { SessionId = sessionId, Response = response });
            }
            catch (HttpOperationException hex)
            {
                // Handle content filter responses gracefully
                _logger.LogWarning(hex, "Content filter blocked the prompt: {Message}", hex.Message);
                return BadRequest(new { Error = "Your message was blocked by content filters. Please rephrase and try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat processing failed: {Message}", ex.Message);

                if (_env.IsDevelopment())
                {
                    return StatusCode(500, new { Error = "An error occurred while processing the input.", Details = ex.Message });
                }

                return StatusCode(500, "An error occurred while processing the input.");
            }
        }
    }
}