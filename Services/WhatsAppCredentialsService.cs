using WhatsAppProject.Data;
using WhatsAppProject.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace WhatsAppProject.Services
{
    public class WhatsAppCredentialsService
    {
        private readonly SaasDbContext _saasContext; // Alterado para o novo contexto

        public WhatsAppCredentialsService(SaasDbContext saasContext)
        {
            _saasContext = saasContext; // Inicializa o contexto SaaS
        }

        public async Task<Sector> GetCredentialsAsync(int sectorId)
        {
            // Busca as credenciais com base no sectorId
            return await _saasContext.Sector
                .FirstOrDefaultAsync(c => c.Id == sectorId);
        }

        public async Task UpdateCredentialsAsync(string phoneNumber, string accessToken, int sectorId)
        {
            var credentials = await GetCredentialsAsync(sectorId);

            if (credentials == null)
            {
                // Insere novas credenciais se não houver nenhuma para o setor
                credentials = new Sector
                {
                    PhoneNumberId = phoneNumber,
                    AccessToken = accessToken,
                };

                _saasContext.Sector.Add(credentials);
            }
            else
            {
                // Atualiza as credenciais existentes
                credentials.PhoneNumberId = phoneNumber;
                credentials.AccessToken = accessToken;
                _saasContext.Sector.Update(credentials);
            }

            await _saasContext.SaveChangesAsync();
        }
    }
}
