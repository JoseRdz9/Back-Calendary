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
    [Route("api/cycles")]
    [Authorize]
    public class CycleController : ControllerBase
    {
        private readonly ICycleService _cycleService;
 
        public CycleController(ICycleService cycleService)
        {
            _cycleService = cycleService;
        }
 
        [HttpGet]
        public async Task<IActionResult> GetCycles()
        {
            var cycles = await _cycleService.GetAllAsync(GetUserId());
            return Ok(cycles);
        }
 
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var cycle = await _cycleService.GetActiveCycleAsync(GetUserId());
            if (cycle == null) return NotFound();
            return Ok(cycle);
        }
 
        [HttpPost("start")]
        public async Task<IActionResult> StartCycle([FromBody] StartCycleRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _cycleService.StartNewCycleAsync(GetUserId(), request.StartDate);
            return Ok(new { message = "Ciclo iniciado correctamente" });
        }
 
        [HttpPost("close")]
        public async Task<IActionResult> CloseCycle([FromBody] CloseCycleRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _cycleService.CloseCurrentCycleAsync(GetUserId(), request.EndDate);
            return Ok(new { message = "Ciclo cerrado correctamente" });
        }
 
        private Guid GetUserId()
            => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}