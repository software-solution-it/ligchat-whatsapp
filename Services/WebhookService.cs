using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WhatsAppProject.Data;
using WhatsAppProject.Entities;
using WhatsAppProject.Dtos;
using System.Collections.Generic;
using System.Text.Json;
using Amazon.S3.Transfer;
using Amazon.S3;

namespace WhatsAppProject.Services
{
    public class WebhookService
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly HttpClient _httpClient;
        private readonly WhatsAppContext _context;
        private readonly SaasDbContext _saasContext;
        private readonly IConfiguration _configuration;
        private readonly WebSocketManager _webSocketManager; // Injete o WebSocketManager

        public WebhookService(
            ILogger<WebhookService> logger,
            WhatsAppContext context,
            SaasDbContext saasContext,
            IConfiguration configuration,
            WebSocketManager webSocketManager) // Adicione o WebSocketManager
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _context = context;
            _saasContext = saasContext;
            _configuration = configuration;
            _webSocketManager = webSocketManager; // Inicialize o WebSocketManager
        }

        private async Task<Sector> GetWhatsAppCredentialsByPhoneNumberIdAsync(string phoneNumberId)
        {
            var credentials = await _saasContext.Sector.FirstOrDefaultAsync(c => c.PhoneNumberId == phoneNumberId);
            if (credentials == null)
            {
                throw new Exception($"Credenciais não encontradas para o número de telefone ID {phoneNumberId}");
            }
            return credentials;
        }

        public async Task<bool> ProcessWebhook(string body)
        {
            try
            {
                _logger.LogInformation("Corpo do webhook recebido: {0}", body);

                // Desserializa o JSON em um Dictionary genérico
                var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

                if (payload != null && payload.TryGetValue("entry", out var entryObj) && entryObj is Newtonsoft.Json.Linq.JArray entryArray && entryArray.Count > 0)
                {
                    var firstEntry = entryArray[0].ToObject<Dictionary<string, object>>();

                    if (firstEntry.TryGetValue("changes", out var changesObj) && changesObj is Newtonsoft.Json.Linq.JArray changesArray && changesArray.Count > 0)
                    {
                        var firstChange = changesArray[0].ToObject<Dictionary<string, object>>();

                        if (firstChange.TryGetValue("value", out var valueObj) && valueObj is Newtonsoft.Json.Linq.JObject changeValue)
                        {
                            // Extrair phone_number_id do metadata
                            var metadata = changeValue["metadata"];
                            var phoneNumberId = metadata["phone_number_id"].ToString();

                            // Obter credenciais usando o phone_number_id
                            var credentials = await GetWhatsAppCredentialsByPhoneNumberIdAsync(phoneNumberId);

                            int contactId = 0; // Variável para armazenar o ID do contato

                            // Extrair informações de contato (nome e número)
                            if (changeValue.TryGetValue("contacts", out var contactsObj) && contactsObj is Newtonsoft.Json.Linq.JArray contactsArray && contactsArray.Count > 0)
                            {
                                var contactInfo = contactsArray[0].ToObject<Newtonsoft.Json.Linq.JObject>();
                                var contactName = contactInfo["profile"]?["name"]?.ToString() ?? "Desconhecido";
                                var contactWaId = contactInfo["wa_id"]?.ToString();

                                // Salvar o contato no banco de dados e obter o ID do contato
                                if (!string.IsNullOrEmpty(contactWaId))
                                {
                                    var contact = await GetOrCreateContactAsync(contactWaId, contactName, credentials.Id);
                                    contactId = contact.Id; // Armazena o ID do contato
                                }
                            }

                            // Processar os statuses (se houver)
                            if (changeValue.TryGetValue("statuses", out var statusesObj) && statusesObj is Newtonsoft.Json.Linq.JArray statusesArray && statusesArray.Count > 0)
                            {
                                await ProcessSentMessage(statusesArray[0].ToObject<Newtonsoft.Json.Linq.JObject>(), credentials);
                            }

                            // Processar as mensagens (se houver)
                            if (changeValue.TryGetValue("messages", out var messagesObj) && messagesObj is Newtonsoft.Json.Linq.JArray messagesArray && messagesArray.Count > 0)
                            {
                                // Passa o ID do contato para ProcessReceivedMessage
                                await ProcessReceivedMessage(messagesArray[0].ToObject<Newtonsoft.Json.Linq.JObject>(), credentials, contactId);
                            }

                            return true; // Indica que o processamento foi bem-sucedido
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError("Erro ao processar JSON: {0}", jsonEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro inesperado: {0}", ex.Message);
            }

            return false; // Retorna false se não processou com sucesso
        }

        private async Task ProcessReceivedMessage(Newtonsoft.Json.Linq.JObject message, Sector credentials, int contactId)
        {
            // Implementar a lógica para processar a mensagem recebida
            string content = "Conteúdo desconhecido";
            string mediaType = null;
            string mediaUrl = null;

            // Processar o conteúdo da mensagem
            if (message.TryGetValue("text", out var textElement))
            {
                content = textElement["body"]?.ToString() ?? content;
                mediaType = "text";
            }
            else if (message.TryGetValue("image", out var imageElement))
            {
                var (url, type) = await GetMediaDetails(credentials, imageElement["id"].ToString());
                mediaUrl = url;
                mediaType = type;
                content = "";
            }
            else if (message.TryGetValue("document", out var documentElement))
            {
                var (url, type) = await GetMediaDetails(credentials, documentElement["id"].ToString());
                mediaUrl = url;
                mediaType = type;
                content = "";
            }
            else if (message.TryGetValue("audio", out var audioElement))
            {
                var (url, type) = await GetMediaDetails(credentials, audioElement["id"].ToString());
                mediaUrl = url;
                mediaType = type;
                content = "";
            }

            // Cria uma nova mensagem e associa ao contato
            var newMessage = new Messages
            {
                Content = content,
                MediaType = mediaType,
                MediaUrl = mediaUrl,
                ContactID = contactId,  // Associar a mensagem ao ID do contato
                SectorId = credentials.Id,
                SentAt = DateTime.UtcNow
            };

            await _context.Messages.AddAsync(newMessage);
            await _context.SaveChangesAsync();

            var messageDto = new MessageReceivedDto
            {
                Id = newMessage.Id,
                Content = newMessage.Content,
                MediaType = newMessage.MediaType,
                MediaUrl = newMessage.MediaUrl,
                ContactID = newMessage.ContactID,
                SentAt = newMessage.SentAt,
                IsSent = newMessage.IsSent
            };

            var messageJson = JsonConvert.SerializeObject(messageDto);

            // Assumindo que você tem acesso ao ID do setor
            string sectorId = credentials.Id.ToString(); // Ou obtenha o ID do setor de outra forma

            // Send the message to the specific sector
            await _webSocketManager.SendMessageToSectorAsync(sectorId, messageJson);
        }
        

        private async Task<Contacts> GetOrCreateContactAsync(string phoneNumber, string name, int sectorId)
        {
            // Verifica se o contato já existe
            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

            if (contact == null)
            {
                // Se o contato não existir, cria um novo
                contact = new Contacts
                {
                    PhoneNumber = phoneNumber,
                    Name = name,
                    SectorId = sectorId,
                    ProfilePictureUrl = null // Caso tenha a URL da foto, pode adicionar
                };

                await _context.Contacts.AddAsync(contact);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Se o contato já existir, você pode opcionalmente atualizar o nome, se desejar
                contact.Name = name;
                _context.Contacts.Update(contact);
                await _context.SaveChangesAsync();
            }

            return contact;
        }

        private async Task ProcessSentMessage(Newtonsoft.Json.Linq.JObject messageStatus, Sector credentials)
        {
            if (messageStatus.TryGetValue("recipient_id", out var recipientId) &&
                messageStatus.TryGetValue("status", out var status))
            {
                _logger.LogInformation($"Mensagem para {recipientId} com status {status} enviada.");
            }
        }

        public async Task<string> UploadMediaToS3Async(string base64File, string mediaType, string originalFileName)
        {
            var awsAccessKey = _configuration["AWS:AccessKey"];
            var awsSecretKey = _configuration["AWS:SecretKey"];
            var awsBucketName = _configuration["AWS:BucketName"];
            var awsRegion = "sa-east-1";

            var s3Client = new AmazonS3Client(awsAccessKey, awsSecretKey, Amazon.RegionEndpoint.GetBySystemName(awsRegion));

            // Decodificar o base64 para bytes
            var fileBytes = Convert.FromBase64String(base64File);

            // Gerar um hash aleatório como nome do arquivo
            string fileHash = GenerateRandomHash();
            string fileExtension = Path.GetExtension(originalFileName);
            string fileName = $"{fileHash}{fileExtension}"; // Nome do arquivo será hash.extensão

            try
            {
                var transferUtility = new TransferUtility(s3Client);

                // Faz upload para o S3 diretamente da memória
                using (var memoryStream = new MemoryStream(fileBytes))
                {
                    var fileTransferRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = awsBucketName,
                        Key = fileName,  // Nome com o hash
                        InputStream = memoryStream,  // Usando MemoryStream ao invés de arquivo no disco
                        ContentType = mediaType
                    };

                    await transferUtility.UploadAsync(fileTransferRequest);
                }

                // Retorna a URL do arquivo no S3
                var fileUrl = $"https://{awsBucketName}.s3.amazonaws.com/{fileName}";

                return fileUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao fazer upload para o S3: {ex.Message}");
            }
        }

        // Método auxiliar para gerar um hash aleatório
        private string GenerateRandomHash()
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                // Gerar um GUID e transformá-lo em bytes
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
                byte[] hashBytes = sha256.ComputeHash(bytes);

                // Converter o hash em string hexadecimal
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private async Task<(string base64Image, string mediaType)> GetMediaDetails(Sector sector, string mediaId)
        {
            if (sector == null || string.IsNullOrEmpty(sector.AccessToken))
            {
                throw new Exception("Credenciais inválidas ou token de acesso ausente.");
            }

            string baseUrl = $"https://graph.facebook.com/v20.0/{mediaId}";

            using (var request = new HttpRequestMessage(HttpMethod.Get, baseUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sector.AccessToken);

                using (var response = await _httpClient.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Erro ao obter os detalhes da mídia: {response.ReasonPhrase}");
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var mediaJson = JsonDocument.Parse(jsonResponse).RootElement;

                    var mediaUrl = mediaJson.GetProperty("url").GetString();
                    var mediaType = mediaJson.GetProperty("mime_type").GetString();

                    if (string.IsNullOrEmpty(mediaUrl) || string.IsNullOrEmpty(mediaType))
                    {
                        throw new Exception("Detalhes da mídia não encontrados.");
                    }

                    // Now let's download the media from mediaUrl and convert it to base64
                    using (var imageRequest = new HttpRequestMessage(HttpMethod.Get, mediaUrl))
                    {
                        imageRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sector.AccessToken);
                        imageRequest.Headers.UserAgent.ParseAdd("curl/7.64.1"); // Add curl user-agent

                        using (var imageResponse = await _httpClient.SendAsync(imageRequest))
                        {
                            // Check the actual content type to ensure it's an image
                            var contentType = imageResponse.Content.Headers.ContentType?.MediaType;
                            if (!imageResponse.IsSuccessStatusCode || !(contentType.StartsWith("image/") || contentType.StartsWith("audio/") || contentType.StartsWith("application/")))
                            {
                                throw new Exception($"Erro ao baixar a mídia. Tipo MIME recebido: {contentType}");
                            }

                            var mediaBytes = await imageResponse.Content.ReadAsByteArrayAsync();

                            // Verifique se o conteúdo realmente foi baixado corretamente
                            if (mediaBytes == null || mediaBytes.Length == 0)
                            {
                                throw new Exception("Falha ao baixar o conteúdo da mídia.");
                            }

                            var mediaBase64 = Convert.ToBase64String(mediaBytes);

                            var fileUrl = await UploadMediaToS3Async(mediaBase64, mediaType, "");

                            return (fileUrl, mediaType);
                        }
                    }
                }
            }
        }
    }
}
