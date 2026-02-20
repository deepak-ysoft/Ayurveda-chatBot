using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ayurveda_chatBot.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession(CreateSessionDto dto)
        {
            var userId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var sessionId = await _chatService.CreateSessionAsync(userId, dto.SessionName);

            return Ok(new { sessionId });
        }
        [HttpPost("send-stream")]
        public async Task SendMessageStream(SendMessageDto dto)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            var userId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            await _chatService.ProcessMessageStream(userId, dto, Response);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var sessions = await _chatService.GetUserSessionsAsync(userId);

            return Ok(sessions);
        }

        [HttpGet("history/{sessionId}")]
        public async Task<IActionResult> GetHistory(Guid sessionId)
        {
            var userId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var history = await _chatService.GetUserHistoryAsync(sessionId);

            return Ok(history);
        }

        [HttpDelete("delete-chat/{chatId}")]
        public async Task<IActionResult> DeleteChat(Guid chatId)
        {
            var res = await _chatService.DeleteChatAsync(chatId);
            return Ok(res);
        }

        [HttpDelete("delete-session/{sessionId}")]
        public async Task<IActionResult> GetSession(Guid sessionId)
        {
            var res = await _chatService.DeleteSessionAsync(sessionId);
            return Ok(res);
        }
    }
}
