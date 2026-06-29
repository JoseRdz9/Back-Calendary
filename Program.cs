using Back_Calendary.Data;
using Back_Calendary.Helpers;
using Back_Calendary.Services.Auth;
using Back_Calendary.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using Back_Calendary.Interfaces.Calendary;
using Back_Calendary.Repositories.Calendary;
using Back_Calendary.Services.Calendary;

var builder = WebApplication.CreateBuilder(args);

// ===================== CONTROLLERS =====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===================== REPOSITORIES =====================
builder.Services.AddScoped<ICycleRepository, CycleRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IPredictionRepository, PredictionRepository>();

// ===================== SERVICES =====================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<ICycleService, CycleService>();
builder.Services.AddScoped<ILogService, LogService>();

builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IPredictionEngine, CyclePredictionEngine>();

// ===================== CONFIG =====================
builder.Services.Configure<SupabaseSettings>(
    builder.Configuration.GetSection("Supabase")
);

// ===================== DB =====================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ===================== CORS =====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===================== SUPABASE CONFIG =====================
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:AnonKey"];

if (string.IsNullOrEmpty(supabaseUrl))
    throw new Exception("Supabase:Url no está configurado");

if (string.IsNullOrEmpty(supabaseKey))
    throw new Exception("Supabase:AnonKey no está configurado");

// ===================== JWKS =====================
var jwksUri = $"{supabaseUrl}/auth/v1/.well-known/jwks.json";

IList<SecurityKey> signingKeys = new List<SecurityKey>();

try
{
    using var httpClient = new HttpClient();

    var jwksJson = await httpClient.GetStringAsync(jwksUri);

    var jwks = new JsonWebKeySet(jwksJson);

    signingKeys = jwks.GetSigningKeys();
}
catch (Exception ex)
{
    Console.WriteLine($"JWKS load error: {ex.Message}");
    signingKeys = new List<SecurityKey>();
}

// ===================== AUTH JWT =====================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,

            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",

            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

// ===================== SUPABASE CLIENT =====================
builder.Services.AddSingleton(_ =>
{
    var client = new Supabase.Client(
        supabaseUrl,
        supabaseKey,
        new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = false
        });

    client.InitializeAsync().GetAwaiter().GetResult();
    return client;
});

var app = builder.Build();

// ===================== PIPELINE =====================
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Render ya maneja HTTPS
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===================== RENDER PORT =====================
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();