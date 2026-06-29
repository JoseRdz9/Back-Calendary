using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Back_Calendary.Interfaces.Calendary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 
namespace Back_Calendary.Controllers.Calendary
{
    [ApiController]
    [Route("api/prediction")]
    [Authorize]
    public class PredictionController : ControllerBase
    {
        private readonly ICycleService _cycleService;
 
        public PredictionController(ICycleService cycleService)
        {
            _cycleService = cycleService;
        }
 
        [HttpGet]
        public async Task<IActionResult> GetPrediction()
        {
            var result = await _cycleService.GetPredictionAsync(GetUserId());
            return Ok(result);
        }
 
        private Guid GetUserId()
            => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}