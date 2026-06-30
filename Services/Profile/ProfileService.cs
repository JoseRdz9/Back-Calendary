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
        
        // ✅ Obtener display_name desde user_metadata
        string? displayName = null;
        string? fileName = null;

        if (doc.TryGetProperty("user_metadata", out var meta))
        {
            // Obtener display_name
            if (meta.TryGetProperty("display_name", out var displayNameProp))
            {
                displayName = displayNameProp.GetString();
            }
            else if (meta.TryGetProperty("full_name", out var fullNameProp))
            {
                // Fallback a full_name si existe
                displayName = fullNameProp.GetString();
            }
            else if (meta.TryGetProperty("name", out var nameProp))
            {
                // Fallback a name
                displayName = nameProp.GetString();
            }

            // Obtener imagen
            if (meta.TryGetProperty("image_url", out var imgProp))
            {
                fileName = imgProp.GetString();
            }
        }

        // Si no hay display_name, usar el email como fallback
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = email?.Split('@')[0] ?? "Usuario";
        }

        // CONVERTIR filename → URL
        var imageUrl = BuildPublicUrl(fileName);

        return new ProfileResponseDto
        {
            Id = userId,
            Email = email,
            DisplayName = displayName, // ✅ Añadir display_name
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

        // ✅ Construir user_metadata con display_name e imagen
        var userMetadata = new Dictionary<string, object>();
        
        // Añadir display_name si se proporciona
        if (!string.IsNullOrEmpty(dto.DisplayName))
        {
            userMetadata["display_name"] = dto.DisplayName;
        }
        else if (!string.IsNullOrEmpty(dto.FullName))
        {
            userMetadata["display_name"] = dto.FullName;
            userMetadata["full_name"] = dto.FullName;
        }
        
        // Añadir imagen si se proporciona
        if (!string.IsNullOrEmpty(newFileName))
        {
            userMetadata["image_url"] = newFileName;
        }
        else if (currentFileName != null)
        {
            // Mantener la imagen actual si no se sube una nueva
            userMetadata["image_url"] = currentFileName;
        }

        var body = new
        {
            email = dto.Email,
            user_metadata = userMetadata
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

        // ✅ Obtener el usuario actualizado
        var updatedUser = await GetProfileAsync(userId);

        return new ProfileResponseDto
        {
            Id = userId,
            Email = dto.Email ?? "",
            DisplayName = updatedUser.DisplayName ?? dto.DisplayName ?? dto.FullName ?? "Usuario",
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