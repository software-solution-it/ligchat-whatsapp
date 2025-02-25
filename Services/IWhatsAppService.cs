using System.Threading.Tasks;
using WhatsAppProject.Dtos;
using WhatsAppProject.Entities;

namespace WhatsAppProject.Services
{
    public interface IWhatsAppService
    {
        Task<Messages> SendTextMessageAsync(SendMessageDto message);
        Task<Messages> SendFileMessageAsync(SendFileDto file);
    }
} 