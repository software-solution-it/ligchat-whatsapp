using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using WhatsAppProject.Entities;
using WhatsAppProject.Services;

namespace WhatsAppProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageSchedullingController : ControllerBase
    {
        private readonly MessageSchedulingService _messageSchedulingService;

        public MessageSchedullingController(MessageSchedulingService messageSchedulingService)
        {
            _messageSchedulingService = messageSchedulingService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<MessageScheduling>> GetAll()
        {
            var result = _messageSchedulingService.GetAllMessageSchedulings();
            return Ok(result);
        }
    }
}
