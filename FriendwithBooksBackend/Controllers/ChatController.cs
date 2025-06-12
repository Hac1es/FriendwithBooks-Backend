using FriendwithBooksBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FriendwithBooksBackend.Services;

namespace FriendwithBooksBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private const int PAGE_SIZE = 20;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // GET: api/Chat/conversations
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int partnerId = 152)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                var messages = await _chatService.GetConversationHistoryAsync(userId, partnerId, page, PAGE_SIZE);
                var totalCount = await _chatService.GetConversationHistoryAsync(userId, partnerId, 1, int.MaxValue);

                return Ok(new
                {
                    messages = messages,
                    totalCount = totalCount.Count(),
                    currentPage = page,
                    totalPages = (int)Math.Ceiling((decimal)(totalCount.Count() / (double)PAGE_SIZE))
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving messages", error = ex.Message });
            }
        }

        // GET: api/Chat/conversations/latest
        [HttpGet("conversations/latest")]
        public async Task<IActionResult> GetLatestMessages([FromQuery] string lastMessageId, [FromQuery] int partnerId = 152)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                var messages = await _chatService.GetLatestMessagesAsync(userId, partnerId, lastMessageId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving latest messages", error = ex.Message });
            }
        }

        //GET: api/Chat/conversations/partners
        [HttpGet("conversations/partners")]
        public async Task<IActionResult> GetChatPartners()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0) return Unauthorized();
                var partners = await _chatService.GetPartnersAsync(userId);
                return Ok(new { partners });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500);
            }
        }

        // DELETE: api/Chat/message/partnerId=...
        [HttpDelete("message")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConversation([FromQuery] int partnerId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                var result = await _chatService.DeleteConversationAsync(userId, partnerId);
                if (result)
                    return Ok(new { success = true, message = "Conversation deleted successfully." });
                else
            return NotFound(new { success = false, message = "Conversation not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error deleting message", error = ex.Message });
            }
        }
    }

    public class SendMessageRequest
    {
        public string Message { get; set; }
        public int RecvId { get; set; }
    }
} 