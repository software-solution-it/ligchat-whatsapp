using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;

namespace WhatsAppProject.Extensions
{
    public static class LoggingExtensions
    {
        public static void LogDetailedException(this ILogger logger, Exception ex, string context)
        {
            logger.LogError($"=== Erro em {context} ===");
            logger.LogError($"Tipo de Exceção: {ex.GetType().FullName}");
            logger.LogError($"Mensagem: {ex.Message}");
            logger.LogError($"StackTrace: {ex.StackTrace}");

            if (ex is HttpRequestException httpEx)
            {
                logger.LogError($"Status Code: {httpEx.StatusCode}");
                logger.LogError($"Base Exception: {httpEx.GetBaseException().Message}");
            }

            if (ex is NotSupportedException)
            {
                logger.LogError("Operação não suportada. Verifique a compatibilidade da operação.");
            }

            var currentEx = ex;
            var depth = 0;
            while (currentEx.InnerException != null && depth < 5)
            {
                currentEx = currentEx.InnerException;
                depth++;
                logger.LogError($"=== Inner Exception Nível {depth} ===");
                logger.LogError($"Tipo: {currentEx.GetType().FullName}");
                logger.LogError($"Mensagem: {currentEx.Message}");
                logger.LogError($"StackTrace: {currentEx.StackTrace}");
            }

            logger.LogError("=== Fim do Log de Exceção ===");
        }

        public static void LogDatabaseError(this ILogger logger, Exception ex, string context)
        {
            if (ex is DbUpdateException dbEx)
            {
                logger.LogError(dbEx, 
                    "Database error in {Context}: {Message}", 
                    context, 
                    dbEx.InnerException?.Message ?? dbEx.Message);

                if (dbEx.InnerException is MySqlConnector.MySqlException sqlEx)
                {
                    logger.LogError(
                        "MySQL error in {Context}: Number={Number}, Message={Message}", 
                        context,
                        sqlEx.Number,
                        sqlEx.Message);
                }

                logger.LogError("Stack trace: {StackTrace}", dbEx.StackTrace);
            }
            else
            {
                logger.LogError(ex, 
                    "Error in {Context}: {Message}", 
                    context, 
                    ex.Message);
            }
        }
    }
} 