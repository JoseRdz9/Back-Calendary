using System.Net.Http.Headers;
using System.Text.Json;

public class ProfileService : IProfileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileStorage _fileStorage;
    private readonly string _supabaseUrl;
    private readonly string _serviceKey;

    public ProfileService(
        IHttpClientFactory httpClientFactory,
        IFileStorage fileStorage,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _fileStorage = fileStorage;
        _supabaseUrl = configuration["Supabase:Url"]!;
        _serviceKey = configuration["Supabase:ServiceRoleKey"]!;
    }

    public async Task<ProfileResponseDto> GetProfileAsync(Guid userId)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _serviceKey);
        client.DefaultRequestHeaders.Add("apikey", _serviceKey);

        var response = await client.GetAsync(
            $"{_supabaseUrl}/auth/v1/admin/users/{userId}"
        );

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        var email = doc.GetProperty("email").GetString() ?? "";
        string imageUrl = null;

        if (doc.TryGetProperty("user_metadata", out var meta) &&
            meta.TryGetProperty("image_url", out var imgProp))
        {
            imageUrl = imgProp.GetString();
        }

        return new ProfileResponseDto
        {
            Id = userId,
            Email = email,
            ImageUrl = imageUrl
        };
    }

    public async Task<ProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        string imageUrl = null;

        if (dto.Image != null)
            imageUrl = await _fileStorage.SaveFileAsync(dto.Image);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _serviceKey);
        client.DefaultRequestHeaders.Add("apikey", _serviceKey);

        var body = new
        {
            email = dto.Email,
            user_metadata = new { image_url = imageUrl }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await client.PutAsync(
            $"{_supabaseUrl}/auth/v1/admin/users/{userId}",
            content
        );

        if (!response.IsSuccessStatusCode)
            throw new Exception("No se pudo actualizar el usuario");

        return new ProfileResponseDto
        {
            Id = userId,
            Email = dto.Email ?? "",
            ImageUrl = imageUrl
        };
    }
}