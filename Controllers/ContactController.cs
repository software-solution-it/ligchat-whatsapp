using Microsoft.AspNetCore.Mvc;
using WhatsAppProject.Services;
using WhatsAppProject.Entities;
using WhatsAppProject.Dtos; // Importar o namespace do ContactDto
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WhatsAppProject.Controllers
{
    [ApiController]
    [Route("contact")]
    public class ContactController : ControllerBase
    {
        private readonly ContactService _contactService;

        public ContactController(ContactService contactService)
        {
            _contactService = contactService;
        }

        /// <summary>
        /// Obtém todos os contatos por setor.
        /// </summary>
        [HttpGet("sector/{sectorId}")] // Modificando a rota para evitar ambiguidade
        public async Task<ActionResult<List<Contacts>>> GetContactsBySectorId(int sectorId)
        {
            var contacts = await _contactService.GetContactsBySectorIdAsync(sectorId);

            if (contacts == null || contacts.Count == 0)
            {
                return NotFound($"Nenhum contato encontrado para o setor com ID {sectorId}.");
            }

            return Ok(contacts);
        }

        /// <summary>
        /// Obtém um contato pelo ID.
        /// </summary>
        [HttpGet("{id}", Name = "GetContactById")]
        public async Task<ActionResult<Contacts>> GetContactById(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id);

            if (contact == null)
            {
                return NotFound($"Contato com ID {id} não encontrado.");
            }

            return Ok(contact);
        }

        /// <summary>
        /// Cria ou atualiza um contato.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Contacts>> CreateOrUpdateContact([FromBody] ContactDto contactDto) // Adiciona [FromBody]
        {

            var createdContact = await _contactService.AddOrUpdateContactAsync(contactDto);
            return CreatedAtRoute("GetContactById", new { id = createdContact.Id }, createdContact);
        }

        /// <summary>
        /// Deleta um contato pelo ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            var contactExists = await _contactService.GetContactByIdAsync(id);
            if (contactExists == null)
            {
                return NotFound($"Contato com ID {id} não encontrado.");
            }

            await _contactService.DeleteContactAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Obtém mensagens por contact_id.
        /// </summary>
        [HttpGet("{contactId}/messages")] // Rota para buscar mensagens
        public async Task<ActionResult<List<Messages>>> GetMessagesByContactId(int contactId)
        {
            var messages = await _contactService.GetMessagesByContactIdAsync(contactId);

            if (messages == null || messages.Count == 0)
            {
                return NotFound($"Nenhuma mensagem encontrada para o contato com ID {contactId}.");
            }

            return Ok(messages);
        }
    }
}
