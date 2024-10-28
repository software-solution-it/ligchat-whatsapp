﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using WhatsAppProject.Services;

namespace WhatsAppProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly WebhookService _webhookService;

        public WebhookController(ILogger<WebhookController> logger, WebhookService webhookService)
        {
            _logger = logger;
            _webhookService = webhookService;
        }

        [HttpGet]
        public IActionResult Get([FromQuery(Name = "hub.mode")] string? hubMode = null,
                                 [FromQuery(Name = "hub.challenge")] string? hubChallenge = null,
                                 [FromQuery(Name = "hub.verify_token")] string? hubVerifyToken = null)
        {
            const string VerifyToken = "121313";  // O token de verificação que você configurou na Meta

            // Verifica se o modo é 'subscribe' e o token está correto
            if (hubMode == "subscribe" && hubVerifyToken == VerifyToken)
            {
                // Retorna o desafio fornecido pela Meta para validar o webhook
                return Ok(hubChallenge);
            }

            // Retorna status 403 se o token de verificação não for válido
            return Forbid();
        }

        [HttpPost]
        public async Task<IActionResult> Webhook([FromBody] JsonElement body)
        {
            try
            {
                var bodyString = body.GetRawText();
                // Chama o serviço para processar o webhook
                var result = await _webhookService.ProcessWebhook(bodyString);

                if (result)
                {
                    return Ok();
                }

                return StatusCode(500, "Erro ao processar o webhook.");
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError("Erro de JSON: {0}", jsonEx.Message);
                return StatusCode(500, "Erro ao processar o webhook.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao processar o webhook: {0}", ex.Message);
                return StatusCode(500, "Erro ao processar o webhook.");
            }
        }
    }
}