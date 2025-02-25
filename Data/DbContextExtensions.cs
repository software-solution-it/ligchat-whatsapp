using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WhatsAppProject.Data
{
    public static class DbContextExtensions
    {
        public static async Task ExecuteWithLoggingAsync<TContext>(
            this TContext context,
            ILogger logger,
            Func<Task> operation,
            string operationName) where TContext : DbContext
        {
            try
            {
                logger.LogInformation($"Iniciando operação {operationName}");
                
                // Log do estado atual do contexto
                foreach (var entry in context.ChangeTracker.Entries())
                {
                    logger.LogDebug($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                }

                await operation();
                
                logger.LogInformation($"Operação {operationName} concluída com sucesso");
            }
            catch (DbUpdateException ex)
            {
                logger.LogError($"Erro de atualização do banco de dados em {operationName}");
                logger.LogError($"Tipo de exceção: {ex.GetType().Name}");
                logger.LogError($"Mensagem: {ex.Message}");
                logger.LogError($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    logger.LogError($"Inner stack trace: {ex.InnerException.StackTrace}");
                }

                throw;
            }
            catch (Exception ex)
            {
                logger.LogError($"Erro não esperado em {operationName}");
                logger.LogError($"Tipo de exceção: {ex.GetType().Name}");
                logger.LogError($"Mensagem: {ex.Message}");
                logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static void EnableDetailedLogging(this DbContext context, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Database");

            context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
            
            context.ChangeTracker.StateChanged += (sender, e) =>
            {
                logger.LogInformation(
                    "Entity {Entity} changed state from {OldState} to {NewState}",
                    e.Entry.Entity.GetType().Name,
                    e.OldState,
                    e.NewState);
            };

            context.ChangeTracker.Tracked += (sender, e) =>
            {
                logger.LogInformation(
                    "Entity {Entity} was tracked in state {State}",
                    e.Entry.Entity.GetType().Name,
                    e.Entry.State);
            };
        }
    }
} 