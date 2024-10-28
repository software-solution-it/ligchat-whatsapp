using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WhatsAppProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly ILogger<TokenController> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private string _accessToken;
        private DateTime _expirationTime;

        public TokenController(ILogger<TokenController> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
        }

        [HttpGet("generate-token")]
        public async Task<IActionResult> GenerateToken()
        {
            try
            {
                var client = _clientFactory.CreateClient();
                var appId = "YOUR_APP_ID";
                var appSecret = "YOUR_APP_SECRET";
                var shortLivedToken = "YOUR_SHORT_LIVED_TOKEN"; // Gera um token usando o Graph Explorer

                // Solicita um novo token de longa duração
                var response = await client.GetAsync(
                    $"https://graph.facebook.com/v13.0/oauth/access_token" +
                    $"?grant_type=fb_exchange_token&client_id={appId}&client_secret={appSecret}&fb_exchange_token={shortLivedToken}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var tokenInfo = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

                    _accessToken = tokenInfo.AccessToken;
                    _expirationTime = DateTime.UtcNow.AddSeconds(tokenInfo.ExpiresIn);

                    // Salva no banco de dados ou em cache, conforme sua necessidade
                    SaveTokenToDatabase(_accessToken, _expirationTime);

                    return Ok(new { token = _accessToken, expires_in = tokenInfo.ExpiresIn });
                }
                else
                {
                    _logger.LogError("Erro ao gerar token: {0}", response.ReasonPhrase);
                    return StatusCode(500, "Erro ao gerar token.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao gerar token: {0}", ex.Message);
                return StatusCode(500, "Erro ao gerar token.");
            }
        }

        [HttpGet("refresh-token")]
        public IActionResult CheckAndRefreshToken()
        {
            if (DateTime.UtcNow.AddMinutes(10) >= _expirationTime)
            {
                // Regenerar token antes que expire
                _logger.LogInformation("Token está prestes a expirar. Gerando novo token...");
                return RedirectToAction("GenerateToken");
            }

            return Ok(new { message = "Token ainda é válido.", expires_at = _expirationTime });
        }

        private void SaveTokenToDatabase(string token, DateTime expirationTime)
        {
            // Implementar lógica para salvar token no banco de dados
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
