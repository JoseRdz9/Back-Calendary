using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Back_Calendary.DTOs.Calendary;
using Back_Calendary.Interfaces.Calendary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 
namespace Back_Calendary.Controllers.Calendary
{
    [ApiController]
    [Route("api/logs")]
    [Authorize]
    public class LogController : ControllerBase
    {
        private readonly ILogService _logService;
 
        public LogController(ILogService logService)
        {
            _logService = logService;
        }
 
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate([FromBody] LogCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _logService.UpsertAsync(GetUserId(), dto);
            return Ok(new { message = "Registro guardado" });
        }
 
        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int days = 30)
        {
            var logs = await _logService.GetRecentAsync(GetUserId(), days);
            return Ok(logs);
        }
 
        private Guid GetUserId()
            => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}