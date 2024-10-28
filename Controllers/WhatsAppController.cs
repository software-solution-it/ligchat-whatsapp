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

        // Envio de mensagem de texto
        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto messageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Enviar mensagem usando o sectorId para escolher as credenciais corretas
            await _whatsappService.SendMessageAsync(messageDto);
            return Ok(new { message = "Mensagem enviada com sucesso!" });
        }

        // Envio de arquivo (imagem, áudio, etc.)
        [HttpPost("send-file")]
        public async Task<IActionResult> SendFile([FromBody] Dtos.SendFileDto sendFileDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Chama o serviço para enviar a mensagem com o arquivo utilizando o sectorId e armazena o resultado
            var mediaResponse = await _whatsappService.SendMediaAsync(sendFileDto);

            // Retorna o JSON com as informações da mídia salva
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
