using System.Net.Http.Headers;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.EntityFrameworkCore;
using WhatsAppProject.Data;
using WhatsAppProject.Dtos;
using WhatsAppProject.Entities;
using Xabe.FFmpeg;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace WhatsAppProject.Services
{
    public class WhatsAppService : IWhatsAppService  
    {
        private readonly WhatsAppContext _context; 
        private readonly SaasDbContext _saasContext;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration; 
        private readonly WebSocketManager _webSocketManager;
        private readonly ILogger<WhatsAppService> _logger;
 
        public WhatsAppService(
            WhatsAppContext context,
            SaasDbContext saasContext,
            HttpClient httpClient,
            IConfiguration configuration,
            WebSocketManager webSocketManager,
            ILogger<WhatsAppService> logger)
        {
            _context = context;
            _saasContext = saasContext;
            _httpClient = httpClient;
            _configuration = configuration;
            _webSocketManager = webSocketManager;
            _logger = logger;
        }

        // Envia mensagem de texto
        public async Task SendMessageAsync(MessageDto messageDto)
        {
            var message = new Messages
            {
                Content = messageDto.Content,
                MediaType = "text",
                MediaUrl = null,
                SectorId = messageDto.SectorId,
                SentAt = DateTime.UtcNow,
                IsSent = true,
                ContactID = messageDto.ContactId
            };

            await _context.Messages.AddAsync(message);

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = messageDto.RecipientPhone,
                type = "text",
                text = new { body = messageDto.Content }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var credentials = await GetWhatsAppCredentialsBySectorIdAsync(messageDto.SectorId);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

            var response = await _httpClient.PostAsync($"https://graph.facebook.com/v20.0/{credentials.PhoneNumberId}/messages", content);
            response.EnsureSuccessStatusCode();

            await _context.SaveChangesAsync();

            var messageJson = JsonSerializer.Serialize(new
            {
                Content = messageDto.Content,
                Recipient = messageDto.RecipientPhone,
                SectorId = messageDto.SectorId,
                IsSent = true,
                ContactID = messageDto.ContactId
            });

            await _webSocketManager.SendMessageToSectorAsync(messageDto.SectorId.ToString(), messageJson);
        }

        public async Task<string> UploadMediaToLocalAsync(string base64File, string mediaType, string originalFileName)
        {
            try
            {
                var fileName = Path.GetFileName(originalFileName);
                
                // Usar o tipo de mídia para determinar a pasta correta
                var relativePath = mediaType switch
                {
                    "image" => $"uploads/image/{fileName}",
                    "audio" => $"uploads/audio/{fileName}",
                    "video" => $"uploads/video/{fileName}",
                    _ => $"uploads/documents/{fileName}"
                };

                var fullPath = Path.Combine("wwwroot", relativePath);
                var directory = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Tratamento de arquivo duplicado
                var counter = 1;
                while (File.Exists(fullPath))
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(fileName);
                    var newFileName = $"{fileNameWithoutExt}({counter}){extension}";
                    relativePath = relativePath.Replace(fileName, newFileName);
                    fullPath = Path.Combine("wwwroot", relativePath);
                    counter++;
                }

                var fileBytes = Convert.FromBase64String(base64File);
                await File.WriteAllBytesAsync(fullPath, fileBytes);
                
                _logger.LogInformation($"Arquivo salvo como {mediaType} em: {fullPath}");
                
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao fazer upload do arquivo: {ex.Message}");
                throw;
            }
        }

        public async Task<object> SendMediaAsync(SendFileDto sendFileDto)
        {
            var fileUrl = await UploadMediaToLocalAsync(
                sendFileDto.Base64File,
                sendFileDto.MediaType,
                sendFileDto.FileName);

            var mediaType = MapMimeTypeToMediaType(sendFileDto.MediaType);
            var credentials = await GetWhatsAppCredentialsBySectorIdAsync(sendFileDto.SectorId);

            var message = new Messages
            {
                Content = sendFileDto.Caption,
                MediaType = mediaType,
                MediaUrl = fileUrl,
                SectorId = sendFileDto.SectorId,
                SentAt = DateTime.UtcNow,
                IsSent = true,
                ContactID = sendFileDto.ContactId
            };

            await _context.Messages.AddAsync(message);

            await SendMediaMessageAsync(fileUrl, sendFileDto.Recipient, mediaType, sendFileDto.FileName, sendFileDto.Caption, sendFileDto.SectorId);

            await _context.SaveChangesAsync();

            var mediaMessageJson = JsonSerializer.Serialize(new
            {
                Content = sendFileDto.Caption,
                MediaType = mediaType,
                MediaUrl = fileUrl,
                IsSent = true,
                ContactID = sendFileDto.ContactId,
                SectorId = sendFileDto.SectorId
            });

            await _webSocketManager.SendMessageToSectorAsync(sendFileDto.SectorId.ToString(), mediaMessageJson);

            return new
            {
                Content = sendFileDto.Caption,
                MediaType = mediaType,
                MediaUrl = fileUrl,
                SectorId = sendFileDto.SectorId,
                ContactID = sendFileDto.ContactId,
                SentAt = message.SentAt,
                IsSent = message.IsSent
            };
        }

        private string GenerateRandomHash()
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
                byte[] hashBytes = sha256.ComputeHash(bytes);

                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private async Task SendMediaMessageAsync(
            string fileUrl,
            string recipient,
            string mediaType,
            string fileName,
            string caption,
            int sectorId)
        {
            var credentials = await GetWhatsAppCredentialsBySectorIdAsync(sectorId);

            object payload;

            switch (mediaType)
            {
                case "audio":
                    payload = new
                    {
                        messaging_product = "whatsapp",
                        to = recipient,
                        type = "audio",
                        audio = new
                        {
                            link = fileUrl
                        }
                    };
                    break;

                case "image":
                    payload = new
                    {
                        messaging_product = "whatsapp",
                        to = recipient,
                        type = "image",
                        image = new { link = fileUrl, caption = caption }
                    };
                    break;

                case "video":
                    payload = new
                    {
                        messaging_product = "whatsapp",
                        to = recipient,
                        type = "video",
                        video = new { link = fileUrl }
                    };
                    break;

                case "document":
                    payload = new
                    {
                        messaging_product = "whatsapp",
                        to = recipient,
                        type = "document",
                        document = new
                        {
                            link = fileUrl,
                            caption = caption,
                            filename = fileName
                        }
                    };
                    break;

                default:
                    throw new ArgumentException("Invalid media type", nameof(mediaType));
            }

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var whatsappBaseUrl = _configuration["WhatsApp:BaseUrl"];

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

            var response = await _httpClient.PostAsync($"{whatsappBaseUrl}/{credentials.PhoneNumberId}/messages", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<Sector> GetWhatsAppCredentialsBySectorIdAsync(int sectorId)
        {
            var credentials = await _saasContext.Sector
                .FirstOrDefaultAsync(c => c.Id == sectorId);

            if (credentials == null)
            {
                throw new Exception($"Credenciais não encontradas para o setor com ID {sectorId}");
            }

            if (string.IsNullOrEmpty(credentials.AccessToken) || string.IsNullOrEmpty(credentials.PhoneNumberId))
            {
                throw new Exception($"Credenciais do WhatsApp não configuradas para o setor {sectorId}");
            }

            return credentials;
        }



        private string MapMimeTypeToMediaType(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType)) return "document";

            mimeType = mimeType.ToLower();

            // Imagens
            if (mimeType.StartsWith("image/") || 
                mimeType.EndsWith(".jpg") || 
                mimeType.EndsWith(".jpeg") || 
                mimeType.EndsWith(".png"))
            {
                return "image";
            }

            // Áudio
            if (mimeType.StartsWith("audio/") || 
                mimeType.EndsWith(".mp3") || 
                mimeType.EndsWith(".ogg") || 
                mimeType.EndsWith(".wav"))
            {
                return "audio";
            }

            // Vídeo
            if (mimeType.StartsWith("video/") || 
                mimeType.EndsWith(".mp4") || 
                mimeType.EndsWith(".avi"))
            {
                return "video";
            }

            return "document";
        }

        private bool IsSupportedAudioFormat(string mimeType)
        {
            var supportedAudioMimeTypes = new List<string>
            {
                "audio/aac",
                "audio/mp4",
                "audio/mpeg",
                "audio/amr",
                "audio/ogg"
            };

            var normalizedMimeType = mimeType.Split(';')[0].ToLower();

            return supportedAudioMimeTypes.Contains(normalizedMimeType);
        }

        public async Task<Messages> SendTextMessageAsync(SendMessageDto message)
        {
            try 
            {
                var credentials = await GetWhatsAppCredentialsBySectorIdAsync(message.SectorId);
                
                // Log para debug
                _logger.LogInformation($"Usando credenciais: Token={credentials.AccessToken}, PhoneId={credentials.PhoneNumberId}");

                var payload = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = message.To,
                    type = "text",
                    text = new { body = message.Text }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Configurar o header de autorização
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

                var url = $"https://graph.facebook.com/v20.0/{credentials.PhoneNumberId}/messages";
                _logger.LogInformation($"Enviando requisição para: {url}");
                _logger.LogInformation($"Payload: {json}");

                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Erro na API do WhatsApp: {errorContent}");
                    throw new Exception($"Erro na API do WhatsApp: {response.StatusCode} - {errorContent}");
                }

                var newMessage = new Messages
                {
                    Content = message.Text,
                    ContactID = message.ContactId,
                    SectorId = message.SectorId,
                    MediaType = "text",
                    SentAt = DateTime.UtcNow,
                    IsSent = true
                };

                await _context.Messages.AddAsync(newMessage);
                await _context.SaveChangesAsync();

                await _webSocketManager.SendMessageToSectorAsync(
                    message.SectorId.ToString(), 
                    JsonSerializer.Serialize(newMessage)
                );

                return newMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao enviar mensagem: {ex.Message}");
                throw;
            }
        }

        public async Task<Messages> SendFileMessageAsync(SendFileDto file)
        {
            try
            {
                var credentials = await GetWhatsAppCredentialsBySectorIdAsync(file.SectorId);
                var mediaType = DetermineMediaType(file.MediaType, file.FileName);

                // Para todos os tipos de arquivo, primeiro fazer upload para S3
                var fileUrl = await UploadMediaToS3Async(
                    file.Base64File,
                    mediaType,
                    file.FileName
                );

                Messages message;

                if (mediaType == "audio")
                {
                    // Converter e fazer upload para WhatsApp
                    var mediaId = await UploadAudioToWhatsAppAsync(
                        file.Base64File,
                        file.FileName,
                        credentials.AccessToken,
                        credentials.PhoneNumberId
                    );

                    // Enviar mensagem usando o ID da mídia
                    var audioPayload = new
                    {
                        messaging_product = "whatsapp",
                        recipient_type = "individual",
                        to = file.Recipient,
                        type = "audio",
                        audio = new { id = mediaId }
                    };

                    var json = JsonSerializer.Serialize(audioPayload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(
                        $"https://graph.facebook.com/v20.0/{credentials.PhoneNumberId}/messages",
                        content
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Erro na API do WhatsApp: {response.StatusCode}");
                    }

                    // Criar mensagem no banco com a URL do S3
                    message = new Messages
                    {
                        Content = file.Caption,
                        MediaType = "audio",
                        MediaUrl = fileUrl, // Usar a URL do S3
                        FileName = file.FileName,
                        SectorId = file.SectorId,
                        SentAt = DateTime.UtcNow,
                        IsSent = true,
                        ContactID = file.ContactId
                    };
                }
                else
                {
                    // Para outros tipos de arquivo
                    message = new Messages
                    {
                        Content = file.Caption,
                        MediaType = mediaType,
                        MediaUrl = fileUrl,
                        FileName = file.FileName,
                        SectorId = file.SectorId,
                        SentAt = DateTime.UtcNow,
                        IsSent = true,
                        ContactID = file.ContactId
                    };

                    // Enviar para o WhatsApp
                    object payload = mediaType switch
                    {
                        "image" => new
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            to = file.Recipient,
                            type = "image",
                            image = new
                            {
                                link = fileUrl,
                                caption = !string.IsNullOrEmpty(file.Caption) ? file.Caption : ""
                            }
                        },
                        "video" => new
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            to = file.Recipient,
                            type = "video",
                            video = new
                            {
                                link = fileUrl
                            }
                        },
                        _ => new
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            to = file.Recipient,
                            type = "document",
                            document = new
                            {
                                link = fileUrl,
                                caption = !string.IsNullOrEmpty(file.Caption) ? file.Caption : "",
                                filename = file.FileName
                            }
                        }
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

                    var response = await _httpClient.PostAsync(
                        $"https://graph.facebook.com/v20.0/{credentials.PhoneNumberId}/messages",
                        content
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Erro na API do WhatsApp: {response.StatusCode}");
                    }
                }

                // Salvar a mensagem no banco de dados
                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();

                // Enviar para o WebSocket
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
                    attachment = new
                    {
                        url = message.MediaUrl,
                        type = message.MediaType,
                        name = message.FileName
                    }
                };

                await _webSocketManager.SendMessageToSectorAsync(
                    file.SectorId.ToString(), 
                    messageData
                );

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao enviar arquivo: {ex.Message}");
                throw;
            }
        }

        private string DetermineMediaType(string mimeType, string fileName)
        {
            // Primeiro, verificar pelo MIME type
            mimeType = (mimeType ?? "").ToLower();
            if (mimeType.StartsWith("audio/") || mimeType.Contains("audio"))
            {
                return "audio";
            }
            
            // Depois, verificar pela extensão do arquivo
            var extension = Path.GetExtension(fileName).ToLower();
            
            switch (extension)
            {
                case ".mp3":
                case ".wav":
                case ".ogg":
                case ".m4a":
                case ".aac":
                    return "audio";
                    
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".webp":
                    return "image";
                    
                case ".mp4":
                case ".avi":
                case ".mov":
                    return "video";
                    
                default:
                    // Se não for nenhum dos tipos acima, verificar novamente o MIME type
                    if (mimeType.StartsWith("image/"))
                        return "image";
                    if (mimeType.StartsWith("video/"))
                        return "video";
                    
                    return "document";
            }
        }

        private async Task<string> UploadMediaToS3Async(string base64File, string mediaType, string fileName)
        {
            try
            {
                var fileBytes = Convert.FromBase64String(base64File);
                string extension;
                string s3Folder;
                string mimeType;

                // Determinar extensão e MIME type corretos
                switch (mediaType.ToLower())
                {
                    case "audio":
                        extension = ".ogg"; // Mudamos de .bin para .ogg
                        s3Folder = "media/audio";
                        mimeType = "audio/ogg"; // MIME type simplificado
                        break;
                    case "image":
                        extension = Path.GetExtension(fileName).ToLower();
                        s3Folder = "media/image";
                        mimeType = GetMimeType(extension);
                        break;
                    default:
                        extension = Path.GetExtension(fileName);
                        s3Folder = "media/documents";
                        mimeType = GetMimeType(extension);
                        break;
                }

                // Gerar nome único para o arquivo com a extensão correta
                var uniqueFileName = $"{s3Folder}/audio_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";

                using var client = new AmazonS3Client(
                    _configuration["AWS:AccessKey"],
                    _configuration["AWS:SecretKey"],
                    Amazon.RegionEndpoint.USEast1
                );

                using var ms = new MemoryStream(fileBytes);
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = ms,
                    Key = uniqueFileName,
                    BucketName = _configuration["AWS:BucketName"],
                    ContentType = mimeType
                };

                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);

                return $"https://{_configuration["AWS:BucketName"]}.s3.amazonaws.com/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao fazer upload para S3: {ex.Message}");
                throw;
            }
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".ogg" => "audio/ogg",  // Simplificado, sem o codecs=opus
                ".opus" => "audio/opus",
                ".mp3" => "audio/mpeg",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".wav" => "audio/wav",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        private async Task<string> UploadAudioToWhatsAppAsync(string base64Audio, string fileName, string accessToken, string phoneNumberId)
        {
            string inputFile = Path.GetTempFileName();
            string outputFile = Path.GetTempFileName() + ".ogg";

            try
            {
                // Converter áudio para OGG/Opus
                var audioBytes = Convert.FromBase64String(base64Audio);
                await File.WriteAllBytesAsync(inputFile, audioBytes);

                // Configurar FFmpeg
                FFmpeg.SetExecutablesPath(@"C:\Program Files\ffmpeg\bin");
                
                // Usar Process diretamente para ter mais controle sobre os argumentos
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(@"C:\Program Files\ffmpeg\bin", "ffmpeg.exe"),
                    Arguments = $"-i \"{inputFile}\" -c:a libopus -b:a 24k -ar 16000 -ac 1 -y \"{outputFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Erro FFmpeg: {error}");
                    throw new Exception($"Erro na conversão do áudio: {error}");
                }

                // Preparar o upload para a API do WhatsApp
                using var form = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(outputFile));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/ogg");
                
                form.Add(fileContent, "file", "audio.ogg");
                form.Add(new StringContent("audio"), "type");
                form.Add(new StringContent("whatsapp"), "messaging_product");

                // Upload para a API do WhatsApp
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.PostAsync(
                    $"https://graph.facebook.com/v20.0/{phoneNumberId}/media",
                    form
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Resposta do upload de mídia: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Erro ao fazer upload do áudio: {response.StatusCode} - {responseContent}");
                }

                // Extrair o ID da mídia da resposta
                var mediaResponse = JsonSerializer.Deserialize<WhatsAppMediaResponse>(responseContent);
                return mediaResponse.Id;
            }
            finally
            {
                // Limpar arquivos temporários
                try
                {
                    if (File.Exists(inputFile)) File.Delete(inputFile);
                    if (File.Exists(outputFile)) File.Delete(outputFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Erro ao limpar arquivos temporários: {ex.Message}");
                }
            }
        }

        public async Task UploadAudio(IFormFile audioFile, double duration)
        {
            // Salvar o arquivo de áudio
            var filePath = Path.Combine("uploads", audioFile.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            // Armazenar a duração no banco de dados ou em outro local
            _logger.LogInformation("Áudio recebido. Duração: {Duration} segundos", duration);

            // Continue com o processamento ou upload para S3
        }
    }



    // DTOs
    public class WhatsAppMediaResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
