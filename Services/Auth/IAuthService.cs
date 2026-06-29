using Back_Calendary.DTOs.Auth;

namespace Back_Calendary.Services.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<AuthResponseDto> RefreshAsync(string refreshToken);
}