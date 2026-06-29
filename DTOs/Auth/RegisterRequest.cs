namespace Back_Calendary.DTOs.Auth;

public class RegisterRequestDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; } = default!;
}