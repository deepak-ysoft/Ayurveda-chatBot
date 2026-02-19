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

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage(SendMessageDto dto)
        {
            var userId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _chatService.ProcessMessage(userId, dto);

            return Ok(result);
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
    }
}
