using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ayurveda_chatBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendMessage(ChatRequestDto model)
        {
            var userId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _chatService.ProcessMessage(userId, model.Message);

            return Ok(result);
        }
    }
}
