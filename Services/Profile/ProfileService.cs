using System.Net.Http.Headers;
using System.Text.Json;

public class ProfileService : IProfileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileStorage _fileStorage;
    private readonly string _supabaseUrl;
    private readonly string _serviceKey;

    private const string Bucket = "profilephotos";

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
        var client = CreateClient();

        var response = await client.GetAsync(
            $"{_supabaseUrl}/auth/v1/admin/users/{userId}"
        );

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        var email = doc.GetProperty("email").GetString() ?? "";

        // 🔥 SOLO filename desde metadata
        string? fileName = null;

        if (doc.TryGetProperty("user_metadata", out var meta) &&
            meta.TryGetProperty("image_url", out var imgProp))
        {
            fileName = imgProp.GetString();
        }

        // 🔥 CONVERTIR filename → URL (UNA SOLA VEZ)
        var imageUrl = BuildPublicUrl(fileName);

        return new ProfileResponseDto
        {
            Id = userId,
            Email = email,
            ImageUrl = imageUrl
        };
    }

    public async Task<ProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var client = CreateClient();

        // Obtener usuario actual para saber si tiene imagen
        var currentUser = await GetProfileAsync(userId);
        string? currentFileName = null;
        
        // Extraer el nombre del archivo de la URL actual
        if (!string.IsNullOrEmpty(currentUser.ImageUrl))
        {
            var uri = new Uri(currentUser.ImageUrl);
            currentFileName = Path.GetFileName(uri.LocalPath);
        }

        string? newFileName = null;

        if (dto.Image != null)
        {
            // Guardar nueva imagen
            newFileName = await _fileStorage.SaveFileAsync(dto.Image);
            
            // Eliminar imagen anterior si existe
            if (!string.IsNullOrEmpty(currentFileName))
            {
                await _fileStorage.DeleteFileAsync(currentFileName);
            }
        }

        var body = new
        {
            email = dto.Email,
            user_metadata = new
            {
                image_url = newFileName // Solo el nombre del archivo
            }
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
            ImageUrl = BuildPublicUrl(newFileName)
        };
    }

    // =======================
    // HELPERS
    // =======================

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _serviceKey);

        client.DefaultRequestHeaders.Add("apikey", _serviceKey);

        return client;
    }

    private string? BuildPublicUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        return $"{_supabaseUrl}/storage/v1/object/public/{Bucket}/{fileName}";
    }
}