using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("Token inválido: no contiene sub");

        if (!Guid.TryParse(userIdClaim, out Guid userId))
            throw new UnauthorizedAccessException("Token inválido: sub no es GUID");

        return userId;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetUserId();

            var user = await _profileService.GetProfileAsync(userId);

            if (user == null)
                return NotFound("Usuario no encontrado");

            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
    {
        try
        {
            Console.WriteLine("📥 [1] Endpoint UpdateProfile llamado");

            var userId = GetUserId();
            Console.WriteLine($"🆔 [2] UserId: {userId}");

            if (dto == null)
            {
                Console.WriteLine("❌ DTO es NULL");
                return BadRequest("DTO inválido");
            }

            Console.WriteLine("📦 [3] DTO recibido");

            Console.WriteLine($"✉️ Email: {dto.Email}");

            if (dto.Image != null)
            {
                Console.WriteLine("🖼️ [4] Imagen RECIBIDA");
                Console.WriteLine($"📛 FileName: {dto.Image.FileName}");
                Console.WriteLine($"📏 Size: {dto.Image.Length}");
            }
            else
            {
                Console.WriteLine("⚠️ [4] Imagen NULL");
            }

            var result = await _profileService.UpdateProfileAsync(userId, dto);

            Console.WriteLine("✅ [5] Perfil actualizado correctamente");

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine("❌ Unauthorized: " + ex.Message);
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error: " + ex.Message);
            return BadRequest(ex.Message);
        }
    }
    }
