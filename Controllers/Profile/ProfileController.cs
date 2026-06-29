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

        // Supabase "sub" viene mapeado por .NET a NameIdentifier
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ??
            User.FindFirst("sub")?.Value;



        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException(
                "Token no contiene user id"
            );
        }



        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {

            Console.WriteLine(
                $"❌ UUID inválido: {userIdClaim}"
            );


            throw new UnauthorizedAccessException(
                "El user id no es GUID válido"
            );
        }



        Console.WriteLine(
            $"✅ USER ID: {userId}"
        );


        return userId;
    }




    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {

        try
        {

            var userId = GetUserId();


            var user =
                await _profileService.GetProfileAsync(userId);



            if (user == null)
            {
                return NotFound(
                    "Usuario no encontrado"
                );
            }



            return Ok(user);

        }
        catch (UnauthorizedAccessException ex)
        {

            return Unauthorized(
                ex.Message
            );

        }
        catch (Exception ex)
        {

            Console.WriteLine(
                "❌ ERROR GET PROFILE: "
                + ex.Message
            );


            return StatusCode(
                500,
                "Error interno"
            );

        }

    }




    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromForm] UpdateProfileDto dto)
    {

        try
        {

            var userId = GetUserId();



            if (dto == null)
            {
                return BadRequest(
                    "DTO inválido"
                );
            }



            var result =
                await _profileService.UpdateProfileAsync(
                    userId,
                    dto
                );



            return Ok(result);

        }
        catch (UnauthorizedAccessException ex)
        {

            return Unauthorized(
                ex.Message
            );

        }
        catch (Exception ex)
        {

            Console.WriteLine(
                "❌ ERROR UPDATE PROFILE: "
                + ex.Message
            );


            return StatusCode(
                500,
                ex.Message
            );

        }

    }
}