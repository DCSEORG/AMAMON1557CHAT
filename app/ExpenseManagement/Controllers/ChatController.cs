using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;
using OpenAI.Chat;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            var conversationHistory = new List<ChatMessage>();
            
            // Convert history from request
            foreach (var msg in request.ConversationHistory)
            {
                if (msg.Role == "user")
                {
                    conversationHistory.Add(new UserChatMessage(msg.Content));
                }
                else if (msg.Role == "assistant")
                {
                    conversationHistory.Add(new AssistantChatMessage(msg.Content));
                }
            }

            var response = await _chatService.ChatAsync(request.Message, conversationHistory);
            return Ok(new { response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Chat endpoint");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryMessage> ConversationHistory { get; set; } = new();
}

public class ChatHistoryMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
