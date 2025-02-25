// Controllers/WhatsAppController.cs
using Microsoft.AspNetCore.Mvc;
using WhatsAppProject.Dtos;
using WhatsAppProject.Services;
using Microsoft.Extensions.Logging;
using WhatsAppProject.Entities;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WhatsAppProject.Controllers
{
    [ApiController]
    [Route("whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<WhatsAppController> _logger;
        private readonly ContactService _contactService;

        public WhatsAppController(
            IWhatsAppService whatsAppService, 
            ILogger<WhatsAppController> logger,
            ContactService contactService)
        {
            _whatsAppService = whatsAppService;
            _logger = logger;
            _contactService = contactService;
        }

        [HttpGet("contacts/{sectorId}")]
        public async Task<IActionResult> GetContacts(int sectorId)
        {
            _logger.LogInformation($"Recebendo requisição GET para /whatsapp/contacts/{sectorId}");
            try
            {
                var contacts = await _contactService.GetContactsBySectorIdAsync(sectorId);
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao buscar contatos: {ex.Message}");
                return StatusCode(500, "Erro ao buscar contatos");
            }
        }

        [HttpGet("messages/{contactId}")]
        public async Task<IActionResult> GetMessages(int contactId)
        {
            _logger.LogInformation($"Recebendo requisição GET para /whatsapp/messages/{contactId}");
            try
            {
                var messages = await _contactService.GetMessagesByContactIdAsync(contactId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao buscar mensagens: {ex.Message}");
                return StatusCode(500, "Erro ao buscar mensagens");
            }
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto message)
        {
            try
            {
                _logger.LogInformation($"Recebida requisição de mensagem: {JsonConvert.SerializeObject(message)}");
                var result = await _whatsAppService.SendTextMessageAsync(message);
                
                var response = new
                {
                    id = result.Id,
                    content = result.Content,
                    contactID = result.ContactID,
                    sectorId = result.SectorId,
                    sentAt = result.SentAt,
                    isSent = result.IsSent,
                    isRead = result.IsRead,
                    mediaType = "text"
                };

                _logger.LogInformation($"Enviando resposta: {JsonConvert.SerializeObject(response)}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao enviar mensagem: {ex.Message}");
                return StatusCode(500, "Erro ao enviar mensagem");
            }
        }

        [HttpPost("send-file")]
        public async Task<IActionResult> SendFile([FromBody] SendFileDto file)
        {
            try
            {
                _logger.LogInformation($"Recebida requisição de arquivo: {JsonConvert.SerializeObject(file)}");
                var result = await _whatsAppService.SendFileMessageAsync(file);
                
                object response;
                if (file.MediaType == "audio" || file.MediaType == "image")
                {
                    response = new
                    {
                        id = result.Id,
                        content = result.Content,
                        contactID = result.ContactID,
                        sectorId = result.SectorId,
                        sentAt = result.SentAt,
                        isSent = result.IsSent,
                        isRead = result.IsRead,
                        mediaType = file.MediaType,
                        mediaUrl = result.MediaUrl,
                        fileName = result.FileName
                    };
                }
                else
                {
                    response = new
                    {
                        id = result.Id,
                        content = result.Content,
                        contactID = result.ContactID,
                        sectorId = result.SectorId,
                        sentAt = result.SentAt,
                        isSent = result.IsSent,
                        isRead = result.IsRead,
                        mediaType = "document",
                        mediaUrl = (string)null,
                        fileName = result.FileName,
                        attachment = new
                        {
                            type = "document",
                            url = result.MediaUrl,
                            name = result.FileName
                        }
                    };
                }

                _logger.LogInformation($"Enviando resposta: {JsonConvert.SerializeObject(response)}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao enviar arquivo: {ex.Message}");
                return StatusCode(500, "Erro ao enviar arquivo");
            }
        }
    }
}
