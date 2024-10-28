using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.EntityFrameworkCore;
using WhatsAppProject.Data;
using WhatsAppProject.Dtos;
using WhatsAppProject.Entities;
using Xabe.FFmpeg;

namespace WhatsAppProject.Services
{
    public class WhatsAppService
    {
        private readonly WhatsAppContext _context; // Contexto para mensagens
        private readonly SaasDbContext _saasContext; // Contexto para acessar o setor
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly WebSocketManager _webSocketManager; // WebSocketManager

        public WhatsAppService(
            WhatsAppContext context,
            SaasDbContext saasContext,
            HttpClient httpClient,
            IConfiguration configuration,
            WebSocketManager webSocketManager)
        {
            _context = context;
            _saasContext = saasContext;
            _httpClient = httpClient;
            _configuration = configuration;
            _webSocketManager = webSocketManager;
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
                to = messageDto.Recipient,
                type = "text",
                text = new { body = messageDto.Content }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Obtém as credenciais do WhatsApp
            var credentials = await GetWhatsAppCredentialsBySectorIdAsync(messageDto.SectorId);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

            var response = await _httpClient.PostAsync($"https://graph.facebook.com/v20.0/{credentials.PhoneNumberId}/messages", content);
            response.EnsureSuccessStatusCode();

            // Salva a mensagem no banco de dados
            await _context.SaveChangesAsync();

            // Enviar a mensagem para o frontend via WebSocket
            var messageJson = JsonSerializer.Serialize(new
            {
                Content = messageDto.Content,
                Recipient = messageDto.Recipient,
                SectorId = messageDto.SectorId,
                IsSent = true,
                ContactID = messageDto.ContactId
            });

            // Enviar mensagem para o setor específico
            await _webSocketManager.SendMessageToSectorAsync(messageDto.SectorId.ToString(), messageJson);
        }

        // Upload de mídia para o S3
        public async Task<string> UploadMediaToS3Async(string base64File, string mediaType, string originalFileName)
        {
            var awsAccessKey = _configuration["AWS:AccessKey"];
            var awsSecretKey = _configuration["AWS:SecretKey"];
            var awsBucketName = _configuration["AWS:BucketName"];
            var awsRegion = "sa-east-1";

            var s3Client = new AmazonS3Client(awsAccessKey, awsSecretKey, Amazon.RegionEndpoint.GetBySystemName(awsRegion));

            // Decodificar o base64 para bytes
            var fileBytes = Convert.FromBase64String(base64File);
            var tempInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}");
            await File.WriteAllBytesAsync(tempInputPath, fileBytes);

            // Se o tipo de mídia não for suportado, não processe com FFmpeg
            if (!IsSupportedAudioFormat(mediaType))
            {
                // Realiza o upload diretamente sem conversão
                var transferUtility = new TransferUtility(s3Client);
                var fileTransferRequest = new TransferUtilityUploadRequest
                {
                    BucketName = awsBucketName,
                    Key = Path.GetFileName(tempInputPath),
                    InputStream = new FileStream(tempInputPath, FileMode.Open, FileAccess.Read),
                    ContentType = mediaType
                };

                await transferUtility.UploadAsync(fileTransferRequest);
                return $"https://{awsBucketName}.s3.amazonaws.com/{Path.GetFileName(tempInputPath)}"; // Retorna a URL do arquivo
            }

            // Definir o caminho de saída para o arquivo convertido
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.opus");

            try
            {
                // Inicializa o FFmpeg
                FFmpeg.SetExecutablesPath(@"C:/Program Files/ffmpeg/bin/");

                // Converter o arquivo de Vorbis para Opus
                await FFmpeg.Conversions.New()
                    .AddParameter($"-i \"{tempInputPath}\" -c:a libopus \"{tempOutputPath}\"", ParameterPosition.PreInput)
                    .Start();

                // Upload do arquivo convertido para o S3
                var transferUtility = new TransferUtility(s3Client);
                var fileTransferRequest = new TransferUtilityUploadRequest
                {
                    BucketName = awsBucketName,
                    Key = Path.GetFileName(tempOutputPath),
                    InputStream = new FileStream(tempOutputPath, FileMode.Open, FileAccess.Read),
                    ContentType = "audio/ogg"
                };

                await transferUtility.UploadAsync(fileTransferRequest);

                // Retorna a URL do arquivo no S3
                return $"https://{awsBucketName}.s3.amazonaws.com/{Path.GetFileName(tempOutputPath)}";
            }
            finally
            {
                // Limpa os arquivos temporários
                if (File.Exists(tempInputPath))
                    File.Delete(tempInputPath);

                if (File.Exists(tempOutputPath))
                    File.Delete(tempOutputPath);
            }
        }
        public async Task<object> SendMediaAsync(SendFileDto sendFileDto)
        {
            // Passo 1: Upload do arquivo base64 para o S3
            var fileUrl = await UploadMediaToS3Async(sendFileDto.Base64File, sendFileDto.MediaType, sendFileDto.FileName);

            var mediaType = MapMimeTypeToMediaType(sendFileDto.MediaType);
            var credentials = await GetWhatsAppCredentialsBySectorIdAsync(sendFileDto.SectorId);

            // Cria a nova mensagem para salvar no banco de dados
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

            // Enviar a mensagem com a mídia
            await SendMediaMessageAsync(fileUrl, sendFileDto.Recipient, mediaType, sendFileDto.FileName, sendFileDto.Caption, sendFileDto.SectorId);

            // Salva a mensagem no banco de dados
            await _context.SaveChangesAsync();

            // Enviar a mensagem com mídia para o frontend via WebSocket
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

            // Retorna as informações da mídia salva
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
            var credentials = await _saasContext.Sector.FirstOrDefaultAsync(c => c.Id == sectorId);
            if (credentials == null)
            {
                throw new Exception($"Credenciais não encontradas para o setor com ID {sectorId}");
            }
            return credentials;
        }



        private string MapMimeTypeToMediaType(string mimeType)
        {
            var normalizedMimeType = mimeType.Split(';')[0];

            if (normalizedMimeType.StartsWith("image/"))
            {
                return "image";
            }
            else if (normalizedMimeType.StartsWith("audio/"))
            {
                return "audio";
            }
            else if (normalizedMimeType.StartsWith("video/"))
            {
                return "video";
            }
            else
            {
                return "document";
            }
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
    }



    // DTOs
    public class WhatsAppMediaResponse
    {
        public List<MediaItem> Media { get; set; }
    }

    public class MediaItem
    {
        public string Id { get; set; }
    }
}
