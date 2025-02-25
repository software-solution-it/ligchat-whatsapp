using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace WhatsAppProject.Middleware
{
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionLoggingMiddleware> _logger;

        public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não tratado: {Message}", ex.Message);
                
                if (ex is DbUpdateException dbEx)
                {
                    _logger.LogError("Database Error: {Message}", dbEx.InnerException?.Message);
                    var sqlException = dbEx.InnerException as MySqlConnector.MySqlException;
                    if (sqlException != null)
                    {
                        _logger.LogError("MySQL Error: Number={Number}, Message={Message}", 
                            sqlException.Number, sqlException.Message);
                    }
                }

                // Log stack trace
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);

                // Log inner exception if exists
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception: {Message}", ex.InnerException.Message);
                    _logger.LogError("Inner Stack Trace: {StackTrace}", ex.InnerException.StackTrace);
                }

                throw;
            }
        }
    }
} 