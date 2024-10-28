using Microsoft.EntityFrameworkCore;
using WhatsAppProject.Data;
using WhatsAppProject.Entities; // Importar a entidade Contacts
using WhatsAppProject.Dtos; // Importar o namespace dos DTOs
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace WhatsAppProject.Services
{
    public class ContactService
    {
        private readonly WhatsAppContext _context;

        public ContactService(WhatsAppContext context)
        {
            _context = context;
        }

        public async Task<List<Contacts>> GetContactsBySectorIdAsync(int sectorId)
        {
            return await _context.Contacts
                .Where(c => c.SectorId == sectorId)
                .ToListAsync();
        }

        public async Task<List<Contacts>> GetContactsByTagIdAsync(string tagId)
        {
            var contacts = await _context.Contacts.ToListAsync();

            // Divide o parâmetro tagId em uma lista de IDs
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

            if (contactDto.Id == 0) // Novo contato
            {
                contact = new Contacts
                {
                    Name = contactDto.Name,
                    PhoneNumber = contactDto.PhoneNumber,
                    ProfilePictureUrl = contactDto.ProfilePictureUrl,
                    SectorId = contactDto.SectorId,
                    // Agora estamos lidando com uma lista de TagIds
                    TagIds = contactDto.TagIds, // Certifique-se de que a propriedade existe
                    Status = contactDto.Status,
                    Address = contactDto.Address,
                    Email = contactDto.Email,
                    Annotations = contactDto.Annotations
                };

                await _context.Contacts.AddAsync(contact);
            }
            else // Atualizar contato existente
            {
                contact = await GetContactByIdAsync(contactDto.Id);

                if (contact != null)
                {
                    contact.Name = contactDto.Name;
                    contact.PhoneNumber = contactDto.PhoneNumber;
                    contact.ProfilePictureUrl = contactDto.ProfilePictureUrl;
                    contact.SectorId = contactDto.SectorId;
                    contact.TagIds = contactDto.TagIds; // Atualiza a lista de TagIds
                    contact.Status = contactDto.Status;
                    contact.Address = contactDto.Address;
                    contact.Email = contactDto.Email;
                    contact.Annotations = contactDto.Annotations;

                    _context.Contacts.Update(contact);
                }
            }

            await _context.SaveChangesAsync();
            return contact; // Retorna o contato criado ou atualizado
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
                .Where(m => m.ContactID == contactId) // Filtra mensagens pelo contact_id
                .ToListAsync();
        }
    }
}
