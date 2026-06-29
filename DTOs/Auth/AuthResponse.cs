namespace Back_Calendary.DTOs.Auth;

public class AuthResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string UserId { get; set; }
    public string? FullName { get; set; }
}