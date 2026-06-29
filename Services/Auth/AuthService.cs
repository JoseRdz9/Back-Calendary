using Back_Calendary.DTOs.Auth;
using Back_Calendary.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Back_Calendary.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly SupabaseSettings _settings;
    private readonly ILogger<AuthService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthService(
        HttpClient http,
        IOptions<SupabaseSettings> settings,
        ILogger<AuthService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    // =====================================================
    // LOGIN
    // =====================================================
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var url = $"{_settings.Url}/auth/v1/token?grant_type=password";

        var body = new
        {
            email = request.Email?.Trim().ToLower(),
            password = request.Password
        };

        var json = await SendRequestAsync(url, body);
        return MapResponseSafe(json);
    }

    // =====================================================
    // REGISTER
    // =====================================================
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var url = $"{_settings.Url}/auth/v1/signup";

        var body = new
        {
            email = request.Email?.Trim().ToLower(),
            password = request.Password,
            data = new
            {
                full_name = request.FullName
            }
        };

        var json = await SendRequestAsync(url, body);
        return MapResponseSafe(json);
    }

    // =====================================================
    // CHANGE PASSWORD
    // =====================================================
    public async Task<bool> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword)
    {
        try
        {
            _logger.LogInformation(
                "Iniciando cambio de contraseña para usuario {UserId}",
                userId);

            var isValid = await VerifyCurrentPasswordAsync(
                userId,
                currentPassword);

            if (!isValid)
            {
                throw new UnauthorizedAccessException(
                    "La contraseña actual es incorrecta");
            }

            var url =
                $"{_settings.Url}/auth/v1/admin/users/{userId}";

            var body = new
            {
                password = newPassword
            };

            var jsonBody = JsonSerializer.Serialize(body);

            using var request =
                new HttpRequestMessage(HttpMethod.Put, url);

            request.Headers.TryAddWithoutValidation("apikey", _settings.ServiceRoleKey);
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _settings.ServiceRoleKey);

            request.Content =
                new StringContent(
                    jsonBody,
                    Encoding.UTF8,
                    "application/json");

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"No se pudo cambiar contraseña: {error}");
            }

            _logger.LogInformation(
                "Contraseña actualizada correctamente para {UserId}",
                userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error en ChangePasswordAsync");

            throw;
        }
    }

    // =====================================================
    // VERIFY CURRENT PASSWORD
    // =====================================================
    private async Task<bool> VerifyCurrentPasswordAsync(
        Guid userId,
        string currentPassword)
    {
        try
        {
            var userUrl = $"{_settings.Url}/auth/v1/admin/users/{userId}";
            // Al inicio de VerifyCurrentPasswordAsync, antes de cualquier request:
            _logger.LogInformation("ServiceRoleKey value: '{Key}'", 
            string.IsNullOrWhiteSpace(_settings.ServiceRoleKey) ? "VACÍO" : _settings.ServiceRoleKey[..20] + "...");

            using var userRequest = new HttpRequestMessage(HttpMethod.Get, userUrl);

            // ✅ TryAddWithoutValidation evita conflictos con headers existentes
            userRequest.Headers.TryAddWithoutValidation("apikey", _settings.ServiceRoleKey);
            userRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ServiceRoleKey);

            var userResponse = await _http.SendAsync(userRequest);
            var userContent = await userResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("Admin user fetch | Status: {Status} | Body: {Body}",
                userResponse.StatusCode, userContent);

            if (!userResponse.IsSuccessStatusCode) return false;

            var doc = JsonDocument.Parse(userContent).RootElement;
            var email = doc.GetProperty("email").GetString();

            if (string.IsNullOrWhiteSpace(email)) return false;

            var loginUrl = $"{_settings.Url}/auth/v1/token?grant_type=password";
            var loginBody = new { email, password = currentPassword };
            var loginJson = JsonSerializer.Serialize(loginBody);

            using var loginRequest = new HttpRequestMessage(HttpMethod.Post, loginUrl);

            // ✅ Mismo fix aquí
            loginRequest.Headers.TryAddWithoutValidation("apikey", _settings.AnonKey);
            loginRequest.Content =
                new StringContent(loginJson, Encoding.UTF8, "application/json");

            var loginResponse = await _http.SendAsync(loginRequest);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("Login verify | Status: {Status} | Body: {Body}",
                loginResponse.StatusCode, loginContent);

            return loginResponse.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando contraseña actual");
            return false;
        }
    }

    // =====================================================
    // CORE REQUEST
    // =====================================================
    private async Task<JsonElement> SendRequestAsync(
        string url,
        object body)
    {
        var jsonBody =
            JsonSerializer.Serialize(body);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                url);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        request.Headers.Add(
            "apikey",
            _settings.AnonKey);

        request.Content =
            new StringContent(
                jsonBody,
                Encoding.UTF8,
                "application/json");

        var response =
            await _http.SendAsync(request);

        var content =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Supabase error ({(int)response.StatusCode}): {content}");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception(
                "Supabase returned empty response");
        }

        return JsonSerializer.Deserialize<JsonElement>(
            content,
            _jsonOptions);
    }

    // =====================================================
    // SAFE RESPONSE MAP
    // =====================================================
    private AuthResponseDto MapResponseSafe(
        JsonElement data)
    {
        if (!data.TryGetProperty(
            "user",
            out var user))
        {
            throw new Exception(
                $"Missing user in response");
        }

        string? fullName = null;

        if (user.TryGetProperty(
            "user_metadata",
            out var metadata))
        {
            if (metadata.TryGetProperty(
                "full_name",
                out var fn))
            {
                fullName = fn.GetString();
            }
        }

        string? accessToken = null;
        string? refreshToken = null;

        if (data.TryGetProperty(
            "access_token",
            out var at))
        {
            accessToken = at.GetString();
        }

        if (data.TryGetProperty(
            "refresh_token",
            out var rt))
        {
            refreshToken = rt.GetString();
        }

        if (data.TryGetProperty(
            "session",
            out var session))
        {
            if (session.TryGetProperty(
                "access_token",
                out var sat))
            {
                accessToken ??= sat.GetString();
            }

            if (session.TryGetProperty(
                "refresh_token",
                out var srt))
            {
                refreshToken ??= srt.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new Exception(
                "Missing access_token");
        }

        return new AuthResponseDto
        {
            UserId =
                user.TryGetProperty(
                    "id",
                    out var id)
                ? id.GetString()
                : null,

            AccessToken = accessToken,
            RefreshToken = refreshToken,
            FullName = fullName
        };
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var url = $"{_settings.Url}/auth/v1/token?grant_type=refresh_token";

        var body = new { refresh_token = refreshToken };
        var json = await SendRequestAsync(url, body);
        return MapResponseSafe(json);
    }
}