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
using System.Text; 
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization; 
using JsonSerializer = System.Text.Json.JsonSerializer;
using Amazon.S3.Model;

namespace WhatsAppProject.Services
{
    public class WebhookService  
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly HttpClient _httpClient;
        private readonly WhatsAppContext _context;
        private readonly SaasDbContext _saasContext;
        private readonly IConfiguration _configuration;
        private readonly WebSocketManager _webSocketManager; 

        public WebhookService(
            ILogger<WebhookService> logger,
            WhatsAppContext context,
            SaasDbContext saasContext,
            IConfiguration configuration,
            WebSocketManager webSocketManager)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _context = context;
            _saasContext = saasContext;
            _configuration = configuration;
            _webSocketManager = webSocketManager; 
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
                var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

                if (payload != null && payload.TryGetValue("entry", out var entryObj) && 
                    entryObj is Newtonsoft.Json.Linq.JArray entryArray && entryArray.Count > 0)
                {
                    var firstEntry = entryArray[0].ToObject<Dictionary<string, object>>();

                    if (firstEntry.TryGetValue("changes", out var changesObj) && 
                        changesObj is Newtonsoft.Json.Linq.JArray changesArray && changesArray.Count > 0)
                    {
                        var firstChange = changesArray[0].ToObject<Dictionary<string, object>>();

                        if (firstChange.TryGetValue("value", out var valueObj) && 
                            valueObj is Newtonsoft.Json.Linq.JObject changeValue)
                        {
                            var metadata = changeValue["metadata"];
                            var phoneNumberId = metadata["phone_number_id"].ToString();

                            var credentials = await GetWhatsAppCredentialsByPhoneNumberIdAsync(phoneNumberId);

                            // Processa mensagens primeiro
                            if (changeValue.TryGetValue("messages", out var messagesObj) && 
                                messagesObj is Newtonsoft.Json.Linq.JArray messagesArray && 
                                messagesArray.Count > 0)
                            {
                                var message = messagesArray[0].ToObject<Newtonsoft.Json.Linq.JObject>();
                                
                                // Obtém o contato
                                int contactId = 0;
                                if (changeValue.TryGetValue("contacts", out var contactsObj) && 
                                    contactsObj is Newtonsoft.Json.Linq.JArray contactsArray && 
                                    contactsArray.Count > 0)
                                {
                                    var contactInfo = contactsArray[0].ToObject<Newtonsoft.Json.Linq.JObject>();
                                    var contactName = contactInfo["profile"]?["name"]?.ToString() ?? "Desconhecido";
                                    var contactWaId = contactInfo["wa_id"]?.ToString();

                                    if (!string.IsNullOrEmpty(contactWaId))
                                    {
                                        var contact = await GetOrCreateContactAsync(contactWaId, contactName, credentials.Id);
                                        contactId = contact.Id;
                                    }
                                }

                                // Processa a mensagem
                                await ProcessReceivedMessage(message, credentials, contactId);
                                return true;
                            }

                            // Processa status depois
                            if (changeValue.TryGetValue("statuses", out var statusesObj) && 
                                statusesObj is Newtonsoft.Json.Linq.JArray statusesArray && 
                                statusesArray.Count > 0)
                            {
                                var status = statusesArray[0].ToObject<Newtonsoft.Json.Linq.JObject>();
                                
                                // Se houver erros no status, loga mas não interrompe o processamento
                                if (status.TryGetValue("errors", out var errorsToken))
                                {
                                    var errors = errorsToken as Newtonsoft.Json.Linq.JArray;
                                    foreach (var error in errors)
                                    {
                                        var errorCode = error["code"]?.ToString();
                                        var errorMessage = error["message"]?.ToString();
                                        _logger.LogWarning($"Status com erro: Código {errorCode} - {errorMessage}");
                                    }
                                }

                                await ProcessSentMessage(status, credentials);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao processar webhook: {ex.Message}");
            }

            return false;
        }

        private Newtonsoft.Json.Linq.JToken GetMediaElement(Newtonsoft.Json.Linq.JObject message)
        {
            try
            {
                var messageType = message["type"]?.ToString()?.ToLower();
                switch (messageType)
                {
                    case "image":
                        return message["image"];
                    case "video":
                        return message["video"];
                    case "audio":
                    case "voice":
                        return message["audio"] ?? message["voice"];
                    case "document":
                        return message["document"];
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao obter elemento de mídia: {ex.Message}");
                return null;
            }
        }

        private async Task ProcessReceivedMessage(Newtonsoft.Json.Linq.JObject message, Sector credentials, int contactId)
        {
            try
            {
                string? content = null;
                string? mediaType = null;
                string? mediaUrl = null;
                string? fileName = null;
                string? mimeType = null;

                var messageType = message["type"]?.ToString();
                _logger.LogInformation($"Tipo de mensagem recebida: {messageType}");

                // Handle text messages
                if (messageType == "text" && message["text"] != null)
                {
                    content = message["text"]["body"]?.ToString();
                    mediaType = "text";
                }
                // Handle media messages (audio, image, video, document)
                else
                {
                    var mediaElement = GetMediaElement(message);
                    if (mediaElement != null)
                    {
                        try
                        {
                            mediaType = DetermineMediaType(messageType);
                            mimeType = mediaElement["mime_type"]?.ToString();
                            fileName = mediaElement["filename"]?.ToString() ?? $"{mediaType}_{DateTime.UtcNow:yyyyMMddHHmmss}{GetFileExtension(mimeType)}";

                            var mediaId = mediaElement["id"]?.ToString();
                            _logger.LogInformation($"Processando mídia: Type={mediaType}, ID={mediaId}, MimeType={mimeType}");

                            if (!string.IsNullOrEmpty(mediaId))
                            {
                                var mediaBytes = await GetMediaBytesFromWhatsApp(credentials, mediaId);
                                if (mediaBytes != null)
                                {
                                    mediaUrl = await UploadToS3(mediaBytes, mediaType, mimeType, fileName);
                                    _logger.LogInformation($"Mídia enviada para S3: {mediaUrl}");

                                    // Para mensagens de documento, pegue a caption se existir
                                    if (messageType == "document" && mediaElement["caption"] != null)
                                    {
                                        content = mediaElement["caption"].ToString();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Erro ao processar mídia: {ex.Message}");
                            throw;
                        }
                    }
                }

                var newMessage = new Messages
                {
                    Content = content,
                    MediaType = mediaType,
                    MediaUrl = mediaUrl,
                    FileName = fileName,
                    MimeType = mimeType,
                    ContactID = contactId,
                    SectorId = credentials.Id,
                    SentAt = DateTime.UtcNow,
                    IsSent = false,
                    IsRead = false
                };

                try
                {
                    _logger.LogInformation($"Salvando mensagem: {JsonConvert.SerializeObject(newMessage)}");
                    await _context.Messages.AddAsync(newMessage);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Mensagem salva com sucesso. ID: {newMessage.Id}");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError($"Erro ao salvar mensagem no banco: {ex.Message}");
                    _logger.LogError($"Inner exception: {ex.InnerException?.Message}");
                    throw;
                }

                var messageData = new
                {
                    id = newMessage.Id,
                    content = newMessage.Content,
                    mediaType = mediaType,
                    mediaUrl = mediaUrl,
                    fileName = fileName,
                    mimeType = mimeType,
                    sectorId = newMessage.SectorId,
                    contactID = newMessage.ContactID,
                    sentAt = newMessage.SentAt,
                    isSent = false,
                    isRead = newMessage.IsRead,
                    attachment = mediaUrl != null ? new
                    {
                        url = mediaUrl,
                        type = mediaType,
                        name = fileName
                    } : null
                };

                await _webSocketManager.SendMessageToSectorAsync(credentials.Id.ToString(), messageData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao processar mensagem: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<Contacts> GetOrCreateContactAsync(string phoneNumber, string name, int sectorId)
        {
            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

            if (contact == null)
            {
                contact = new Contacts
                {
                    PhoneNumber = phoneNumber,
                    Name = name,
                    SectorId = sectorId,
                    ProfilePictureUrl = null
                };

                await _context.Contacts.AddAsync(contact);
                await _context.SaveChangesAsync();
            }
            else
            {
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

        private async Task<byte[]> GetMediaBytesFromWhatsApp(Sector credentials, string mediaId)
        { 
            try
            {
                _logger.LogInformation($"Iniciando download de mídia ID: {mediaId}");
                
                // 1. Obter informações da mídia
                var mediaUrl = $"https://graph.facebook.com/v21.0/{mediaId}";
                
                using var request = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var client = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                });

                var mediaResponse = await client.SendAsync(request);
                mediaResponse.EnsureSuccessStatusCode();

                var mediaInfo = await mediaResponse.Content.ReadAsStringAsync();
                _logger.LogInformation($"Informações da mídia: {mediaInfo}");
                
                var mediaData = JsonSerializer.Deserialize<WhatsAppMediaResponse>(mediaInfo);
                
                if (mediaData == null || string.IsNullOrEmpty(mediaData.Url))
                {
                    throw new Exception("Dados da mídia não encontrados na resposta");
                }

                // 2. Download da mídia
                using var downloadRequest = new HttpRequestMessage(HttpMethod.Get, mediaData.Url);
                downloadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
                downloadRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                downloadRequest.Headers.Add("User-Agent", "WhatsApp/2.0");

                // Usar um novo HttpClient com configurações específicas para download
                using var downloadClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                });

                downloadClient.Timeout = TimeSpan.FromMinutes(5); // Aumentar timeout para arquivos grandes

                var downloadResponse = await downloadClient.SendAsync(downloadRequest);
                downloadResponse.EnsureSuccessStatusCode();

                var mediaBytes = await downloadResponse.Content.ReadAsByteArrayAsync();

                if (mediaBytes.Length == 0)
                {
                    throw new Exception("Download da mídia resultou em 0 bytes");
                }

                // Verificar se o tamanho está muito diferente do esperado
                if (mediaData.FileSize > 0 && mediaBytes.Length < mediaData.FileSize * 0.9)
                {
                    _logger.LogWarning($"Tentando download novamente devido ao tamanho incorreto");
                    
                    // Segunda tentativa com configurações diferentes
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Get, mediaData.Url);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
                    retryRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaData.MimeType));
                    retryRequest.Headers.Add("User-Agent", "WhatsApp/2.0");

                    using var retryClient = new HttpClient(new HttpClientHandler
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.None,
                        AllowAutoRedirect = true,
                        MaxAutomaticRedirections = 10
                    })
                    {
                        Timeout = TimeSpan.FromMinutes(5)
                    };

                    var retryResponse = await retryClient.SendAsync(retryRequest);
                    retryResponse.EnsureSuccessStatusCode();

                    var contentLength = retryResponse.Content.Headers.ContentLength;
                    _logger.LogInformation($"Tamanho do conteúdo informado: {contentLength}");

                    // Usar MemoryStream para garantir que todos os bytes sejam lidos
                    using var ms = new MemoryStream();
                    await retryResponse.Content.CopyToAsync(ms);
                    mediaBytes = ms.ToArray();

                    _logger.LogInformation($"Tamanho após segunda tentativa: {mediaBytes.Length}");
                }

                _logger.LogInformation($"Download concluído com sucesso. Tamanho final: {mediaBytes.Length} bytes");
                
                return mediaBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao obter mídia do WhatsApp: {ex.Message}");
                throw;
            }
        }

        private string CalculateSha256(byte[] bytes)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private class WhatsAppMediaResponse
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("mime_type")]
            public string MimeType { get; set; }

            [JsonPropertyName("sha256")]
            public string Sha256 { get; set; }

            [JsonPropertyName("file_size")]
            public long FileSize { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("messaging_product")]
            public string MessagingProduct { get; set; }
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Dispara um evento para o callbackUrl do webhook especificado.
        /// </summary>
        /// <param name="sectorId">Identificador do setor.</param>
        /// <param name="eventData">Os dados a serem enviados para o webhook.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
        public async Task<bool> TriggerWebhookEventAsync(int sectorId, object eventData)
        {
            try 
            {
                var message = JsonConvert.SerializeObject(eventData, new JsonSerializerSettings 
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                _logger.LogInformation($"Enviando mensagem para setor {sectorId}: {message}");
                
                await _webSocketManager.SendMessageToSectorAsync(sectorId.ToString(), message);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao enviar mensagem WebSocket: {ex.Message}");
                return false;
            }
        }

        private string DetermineNormalizedMediaType(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType)) return "text";

            var normalizedMimeType = mimeType.ToLower().Trim();
            
            _logger.LogInformation($"Determinando tipo de mídia para MIME: {normalizedMimeType}");

            // Verifica primeiro por tipos específicos
            if (normalizedMimeType.StartsWith("image/")) 
            {
                return "image";
            }
            if (normalizedMimeType.StartsWith("audio/") || normalizedMimeType.EndsWith(".ogg")) 
            {
                return "audio";
            }
            if (normalizedMimeType.StartsWith("video/")) 
            {
                return "video";
            }

            // Executáveis e documentos
            if (normalizedMimeType.Contains("exe") || 
                normalizedMimeType.Contains("x-msdownload") ||
                normalizedMimeType.Contains("x-executable") ||
                normalizedMimeType.Contains("application/octet-stream"))
            {
                return "document";
            }

            return "document";
        }

        private string DetermineFileExtension(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return ".bin";

            return mimeType.ToLower() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "audio/mp3" => ".mp3",
                "audio/mpeg" => ".mp3",
                "audio/ogg" => ".ogg",
                "audio/wav" => ".wav",
                "video/mp4" => ".mp4",
                "video/mpeg" => ".mpeg",
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                _ => mimeType.Contains("image/") ? ".jpg" : ".bin"
            };
        }

        private string DetermineMediaType(string messageType)
        {
            if (string.IsNullOrEmpty(messageType))
                return "text";

            return messageType.ToLower() switch
            {
                "image" => "image",
                "video" => "video",
                "audio" => "audio",
                "document" => "document",
                "voice" => "audio",
                "sticker" => "image",
                _ => "text"
            };
        }

        private string GetFileExtension(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return ".bin";

            return mimeType.ToLower() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "audio/mpeg" => ".mp3",
                "audio/mp3" => ".mp3",
                "audio/ogg" => ".ogg",
                "audio/opus" => ".opus",
                "audio/wav" => ".wav",
                "video/mp4" => ".mp4",
                "video/mpeg" => ".mpeg",
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                _ => mimeType.StartsWith("image/") ? ".jpg" : ".bin"
            };
        }

        private async Task<string> UploadToS3(byte[] mediaBytes, string mediaType, string mimeType, string fileName)
        {
            try
            {
                var bucketName = _configuration["AWS:BucketName"];
                var s3Key = $"media/{mediaType}/{fileName}".Replace("//", "/").TrimStart('/');

                var s3Config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.USEast1,
                    ForcePathStyle = true
                };

                using var client = new AmazonS3Client(
                    _configuration["AWS:AccessKey"],
                    _configuration["AWS:SecretKey"],
                    s3Config
                );

                using var ms = new MemoryStream(mediaBytes);
                
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = ms,
                    Key = s3Key,
                    BucketName = bucketName,
                    ContentType = mimeType,
                    AutoCloseStream = true
                };

                // Adiciona metadados
                uploadRequest.Metadata.Add("original-filename", fileName);
                uploadRequest.Metadata.Add("media-type", mediaType);
                uploadRequest.Metadata.Add("mime-type", mimeType);
                uploadRequest.Metadata.Add("content-length", mediaBytes.Length.ToString());

                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);

                var mediaUrl = $"https://{bucketName}.s3.amazonaws.com/{s3Key}";
                _logger.LogInformation($"Arquivo salvo no S3: {mediaUrl}");
                _logger.LogInformation($"Detalhes do upload: Filename={fileName}, MimeType={mimeType}, MediaType={mediaType}, Size={mediaBytes.Length}");

                return mediaUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao fazer upload para S3: {ex.Message}");
                throw;
            }
        }

        private async Task<Messages> SaveMessageToDatabase(string content, string mediaType, string mediaUrl, string fileName, string mimeType, int sectorId, int contactId)
        {
            try 
            {
                var message = new Messages
                {
                    Content = content ?? fileName ?? "Mídia sem descrição",
                    MediaType = mediaType,
                    MediaUrl = mediaUrl,
                    FileName = fileName,
                    MimeType = mimeType,
                    SectorId = sectorId,
                    ContactID = contactId,
                    SentAt = DateTime.UtcNow,
                    IsSent = true,
                    IsRead = false
                };

                _logger.LogInformation($"Salvando mensagem: {JsonSerializer.Serialize(message)}");
                
                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();

                var messageData = new
                {
                    id = message.Id,
                    content = message.Content,
                    mediaType = message.MediaType,
                    mediaUrl = message.MediaUrl,
                    fileName = message.FileName,
                    mimeType = message.MimeType,
                    sectorId = message.SectorId,
                    contactID = message.ContactID,
                    sentAt = message.SentAt,
                    isSent = message.IsSent,
                    isRead = message.IsRead,
                    attachment = message.MediaUrl != null ? new
                    {
                        url = message.MediaUrl,
                        type = message.MediaType,
                        name = message.FileName
                    } : null
                };

                await _webSocketManager.SendMessageToSectorAsync(sectorId.ToString(), messageData);
                
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao salvar mensagem no banco: {ex.Message}");
                _logger.LogError($"Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }
    }
}
