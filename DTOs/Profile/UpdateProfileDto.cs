using Microsoft.AspNetCore.Http;

public class UpdateProfileDto
{
    public string? Email {get; set;}
    public IFormFile? Image {get; set;}
    public string? DisplayName { get; set; }
    public string? FullName { get; set; }
}