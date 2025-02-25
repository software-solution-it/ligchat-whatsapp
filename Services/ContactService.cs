using Microsoft.EntityFrameworkCore;
using WhatsAppProject.Data;
using WhatsAppProject.Entities; // Importar a entidade Contacts
using WhatsAppProject.Dtos; // Importar o namespace dos DTOs
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace WhatsAppProject.Services
{
    public class ContactService
    {
        private readonly WhatsAppContext _context;
        private readonly ILogger<ContactService> _logger;

        public ContactService(WhatsAppContext context, ILogger<ContactService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Contacts>> GetContactsBySectorIdAsync(int sectorId)
        {
            try
            {
                var contacts = await _context.Contacts
                    .Where(c => c.SectorId == sectorId)
                    .Select(c => new Contacts
                    {
                        Id = c.Id,
                        Name = c.Name,
                        PhoneNumber = c.PhoneNumber,
                        ProfilePictureUrl = c.ProfilePictureUrl,
                        SectorId = c.SectorId,
                        TagIds = c.TagIds,
                        Status = c.Status,
                        Address = c.Address,
                        Email = c.Email,
                        Annotations = c.Annotations
                    })
                    .ToListAsync();

                return contacts ?? new List<Contacts>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao buscar contatos: {ex.Message}");
                return new List<Contacts>();
            }
        }

        public async Task<List<Contacts>> GetContactsByTagIdAsync(string tagId)
        {
            var contacts = await _context.Contacts.ToListAsync();

            var tagIdsToFind = tagId.Split(',');

            return contacts
                .Where(c => c.TagIds != null && c.TagIds.Split(',').Any(id => tagIdsToFind.Contains(id)))
                .ToList();
        }



        public async Task<Contacts> GetContactByIdAsync(int id)
        {
            return await _context.Contacts.FindAsync(id);
        }

        public async Task<Contacts> AddOrUpdateContactAsync(ContactDto contactDto)
        {
            Contacts contact;

            if (contactDto.Id == 0) 
            {
                contact = new Contacts
                {
                    Name = contactDto.Name,
                    PhoneNumber = contactDto.PhoneNumber,
                    ProfilePictureUrl = contactDto.ProfilePictureUrl,
                    SectorId = contactDto.SectorId,
                    TagIds = contactDto.TagIds, 
                    Status = contactDto.Status,
                    Address = contactDto.Address,
                    Email = contactDto.Email,
                    Annotations = contactDto.Annotations
                };

                await _context.Contacts.AddAsync(contact);
            }
            else
            {
                contact = await GetContactByIdAsync(contactDto.Id);

                if (contact != null)
                {
                    contact.Name = contactDto.Name;
                    contact.PhoneNumber = contactDto.PhoneNumber;
                    contact.ProfilePictureUrl = contactDto.ProfilePictureUrl;
                    contact.SectorId = contactDto.SectorId;
                    contact.TagIds = contactDto.TagIds; 
                    contact.Status = contactDto.Status;
                    contact.Address = contactDto.Address;
                    contact.Email = contactDto.Email;
                    contact.Annotations = contactDto.Annotations;

                    _context.Contacts.Update(contact);
                }
            }

            await _context.SaveChangesAsync();
            return contact; 
        }

        public async Task DeleteContactAsync(int id)
        {
            var contact = await GetContactByIdAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Messages>> GetMessagesByContactIdAsync(int contactId)
        {
            return await _context.Messages
                .Where(m => m.ContactID == contactId) 
                .ToListAsync();
        }
    }
}
