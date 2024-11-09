// Controllers/WhatsAppController.cs
using Microsoft.AspNetCore.Mvc;
using WhatsAppProject.Dtos;
using WhatsAppProject.Services;

namespace WhatsAppProject.Controllers
{
    [ApiController]
    [Route("whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private readonly WhatsAppService _whatsappService;

        public WhatsAppController(WhatsAppService whatsappService)
        {
            _whatsappService = whatsappService;
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto messageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _whatsappService.SendMessageAsync(messageDto);
            return Ok(new { message = "Mensagem enviada com sucesso!" });
        }

        [HttpPost("send-file")]
        public async Task<IActionResult> SendFile([FromBody] Dtos.SendFileDto sendFileDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var mediaResponse = await _whatsappService.SendMediaAsync(sendFileDto);

            return Ok(new
            {
                message = "Arquivo enviado com sucesso!",
                media = mediaResponse
            });
        }


        public class WhatsAppMediaResponse
        {
            public string Id { get; set; }
        }


    }


}
