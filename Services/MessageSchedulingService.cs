using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using WhatsAppProject.Data;
using WhatsAppProject.Entities;
using WhatsAppProject.Dtos;
using MongoDB.Driver;
using WhatsAppProject.Entities.WhatsAppProject.Entities;
using MongoDB.Bson;

namespace WhatsAppProject.Services
{
    public class MessageSchedulingService
    {
        private readonly SaasDbContext _saasContext;
        private readonly MongoDbContext _mongoDbContext;
        private readonly ContactService _contactService;
        private readonly WhatsAppService _whatsappService;

        public MessageSchedulingService(SaasDbContext context, MongoDbContext mongoDbContext, ContactService contactService, WhatsAppService whatsappService)
        {
            _saasContext = context;
            _mongoDbContext = mongoDbContext;
            _contactService = contactService;
            _whatsappService = whatsappService;
        }

        public async Task ScheduleAllMessagesAsync()
        {
            var messageSchedulings = GetAllMessageSchedulings();

            foreach (var message in messageSchedulings)
            {
                var contacts = await _contactService.GetContactsByTagIdAsync(message.Tags);

                foreach (var contact in contacts)
                {
                    if (message.FlowId == null)
                    {
                        if (!IsMessageSentToContact(contact.Id, message.Id))
                        {
                            EnqueueMessageForScheduledTime(message, contact);
                        }
                    }
                    else
                    {
                        await EnqueueMessageForFluxAsync(message, contact);
                    }
                }
            }
        }

        public List<MessageScheduling> GetAllMessageSchedulings()
        {
            return _saasContext.MessageScheduling.ToList();
        }

        public void EnqueueMessageForScheduledTime(MessageScheduling message, Contacts contact)
        {
            if (DateTime.TryParseExact(message.SendDate, "dd-MM-yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime scheduledDateTime))
            {
                var delay = scheduledDateTime - DateTime.Now;

                if (delay > TimeSpan.Zero)
                {
                    BackgroundJob.Schedule(() => SendMessageAsync(message, contact), delay);
                    Console.WriteLine($"Mensagem agendada para {contact.Name} ({contact.Id}) às {scheduledDateTime}");
                }
                else
                {
                    Console.WriteLine($"Horário de envio para {contact.Name} já passou. Job não enfileirado.");
                }
            }
            else
            {
                Console.WriteLine($"Formato de data inválido para {message.SendDate}. Job não enfileirado.");
            }
        }

        public async Task EnqueueMessageForFluxAsync(MessageScheduling message, Contacts contact)
        {
            var contactFlowStatus = _saasContext.ContactFlowStatus
                .FirstOrDefault(status => status.ContactId == contact.Id && status.FlowId == message.FlowId);

            if (contactFlowStatus == null)
            {
                contactFlowStatus = new ContactFlowStatus
                {
                    ContactId = contact.Id,
                    FlowId = message.FlowId,
                    CurrentNodeId = "",
                    IsFlowComplete = false
                };
                _saasContext.ContactFlowStatus.Add(contactFlowStatus);
                _saasContext.SaveChanges();
            }

            if (!contactFlowStatus.IsFlowComplete)
            {
                try
                {
                    var flowIdString = message.FlowId;
                    var flow = await _mongoDbContext.Flows
                        .Find(Builders<FlowDTO>.Filter.Eq(f => f.Id, flowIdString))
                        .FirstOrDefaultAsync();

                    if (flow != null)
                    {
                        // Se CurrentNodeId estiver vazio, defina como o primeiro nó, mas não envie mensagem ainda
                        if (string.IsNullOrEmpty(contactFlowStatus.CurrentNodeId))
                        {
                            var firstNode = flow.Nodes.FirstOrDefault();
                            if (firstNode != null)
                            {
                                contactFlowStatus.CurrentNodeId = firstNode.Id;
                                _saasContext.ContactFlowStatus.Update(contactFlowStatus);
                                _saasContext.SaveChanges();

                                // Não enviar mensagem aqui, apenas definir o nó atual
                            }
                        }
                        else
                        {
                            var currentNode = flow.Nodes.FirstOrDefault(node => node.Id == contactFlowStatus.CurrentNodeId);

                            if (currentNode != null)
                            {
                                var messageDto = new MessageDto
                                {
                                    Content = currentNode.Blocks.FirstOrDefault()?.Content ?? string.Empty,
                                    Recipient = contact.PhoneNumber,
                                    ContactId = contact.Id
                                };

                                await _whatsappService.SendMessageAsync(messageDto);
                                WaitForUserResponse(contact, flow, contactFlowStatus);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Fluxo não encontrado com ID: {message.FlowId}");
                    }
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Erro de formato ao buscar o fluxo: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao buscar o fluxo: {ex.Message}");
                }
            }
        }

        private void WaitForUserResponse(Contacts contact, FlowDTO flow, ContactFlowStatus contactFlowStatus)
        {
            // Achar o índice do nó atual no fluxo
            var currentNodeIndex = flow.Nodes.FindIndex(node => node.Id == contactFlowStatus.CurrentNodeId);

            // Pular o primeiro nó, que é sempre o início
            if (string.IsNullOrEmpty(contactFlowStatus.CurrentNodeId))
            {
                currentNodeIndex = 0; // Começar do primeiro nó
            }
            else
            {
                // Mover para o próximo nó, ignorando o primeiro
                currentNodeIndex += 1;
            }

            if (currentNodeIndex >= 0 && currentNodeIndex < flow.Nodes.Count)
            {
                var nextNode = flow.Nodes[currentNodeIndex];
                contactFlowStatus.CurrentNodeId = nextNode.Id;
                contactFlowStatus.UpdatedAt = DateTime.UtcNow;
                _saasContext.ContactFlowStatus.Update(contactFlowStatus);
                _saasContext.SaveChanges();

                var messageDto = new MessageDto
                {
                    Content = nextNode.Blocks.FirstOrDefault()?.Content ?? string.Empty,
                    Recipient = contact.PhoneNumber,
                    ContactId = contact.Id
                };

                _whatsappService.SendMessageAsync(messageDto).Wait();
            }
            else if (currentNodeIndex >= flow.Nodes.Count)
            {
                contactFlowStatus.IsFlowComplete = true;
                contactFlowStatus.UpdatedAt = DateTime.UtcNow;
                _saasContext.ContactFlowStatus.Update(contactFlowStatus);
                _saasContext.SaveChanges();

                Console.WriteLine($"Fluxo concluído para contato {contact.Id}");
            }
        }


        public async Task SendMessageAsync(MessageScheduling message, Contacts contact)
        {
            var messageDto = MapToMessageDto(message, contact);
            await _whatsappService.SendMessageAsync(messageDto);

            if (message.FileAttachment != null || message.FileMimeType != null || message.FileName != null)
            {
                var fileDto = MapToFileDto(message, contact);
                await _whatsappService.SendMediaAsync(fileDto);
            }

            if (message.ImageAttachment != null && message.ImageMimeType != null && message.ImageName != null)
            {
                var fileDto = MapToFileImageDto(message, contact);
                await _whatsappService.SendMediaAsync(fileDto);
            }

            await MarkMessageAsSent(contact.Id, message.Id);
            Console.WriteLine($"Mensagem enviada para {contact.Name} ({contact.Id}) com sucesso.");
        }

        private bool IsMessageSentToContact(int contactId, int messageId)
        {
            return _saasContext.ContactMessageStatus
                .Any(status => status.ContactId == contactId && status.MessageSchedulingId == messageId && status.IsSent);
        }

        private async Task MarkMessageAsSent(int contactId, int messageId)
        {
            var status = new ContactMessageStatus
            {
                ContactId = contactId,
                MessageSchedulingId = messageId,
                IsSent = true,
                SentAt = DateTime.UtcNow
            };

            _saasContext.ContactMessageStatus.Add(status);
            await _saasContext.SaveChangesAsync();
        }

        public SendFileDto MapToFileImageDto(MessageScheduling message, Contacts contact)
        {
            return new SendFileDto
            {
                Base64File = message.ImageAttachment ?? message.FileAttachment,
                MediaType = message.ImageMimeType ?? message.FileMimeType,
                FileName = message.ImageName ?? message.FileName,
                Caption = "",
                Recipient = contact.PhoneNumber,
                ContactId = contact.Id,
                SectorId = message.SectorId ?? 0
            };
        }

        public SendFileDto MapToFileDto(MessageScheduling message, Contacts contact)
        {
            return new SendFileDto
            {
                Base64File = message.FileAttachment,
                MediaType = message.FileMimeType,
                FileName = message.FileName,
                Caption = "",
                Recipient = contact.PhoneNumber,
                ContactId = contact.Id,
                SectorId = message.SectorId ?? 0
            };
        }

        public MessageDto MapToMessageDto(MessageScheduling message, Contacts contact)
        {
            return new MessageDto
            {
                Content = message.MessageText ?? string.Empty,
                MediaType = message.ImageMimeType ?? message.FileMimeType,
                MediaUrl = message.ImageAttachment ?? message.FileAttachment,
                SectorId = message.SectorId ?? 0,
                Recipient = contact.PhoneNumber,
                ContactId = contact.Id
            };
        }
    }
}
