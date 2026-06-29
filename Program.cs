using Back_Calendary.Data;
using Back_Calendary.Helpers;
using Back_Calendary.Services.Auth;
using Back_Calendary.Settings;
// using Back_Calendary.Services.Profile;
// using Back_Calendary.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Http;
using Supabase;
using Back_Calendary.Interfaces.Calendary;
using Back_Calendary.Repositories.Calendary;
using Back_Calendary.Services.Calendary;


var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Repositorios
builder.Services.AddScoped<ICycleRepository, CycleRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IPredictionRepository, PredictionRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICycleService, CycleService>();
builder.Services.AddScoped<ILogService, LogService>();

// Motor de predicción — singleton porque no guarda estado
builder.Services.AddSingleton<IPredictionEngine, CyclePredictionEngine>();


builder.Services.Configure<SupabaseSettings>(
    builder.Configuration.GetSection("Supabase")
);

builder.Services.AddHttpClient<IAuthService, AuthService>();



// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// AUTH JWT
var supabaseUrl = builder.Configuration["Supabase:Url"];
var jwksUri = $"{supabaseUrl}/auth/v1/.well-known/jwks.json";

// Descargar JWKS manualmente al arrancar
var httpClient = new HttpClient();
var jwksJson = await httpClient.GetStringAsync(jwksUri);
var jwks = new JsonWebKeySet(jwksJson);
var signingKeys = jwks.GetSigningKeys();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,  // claves ES256 cargadas
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = false,
            ValidateLifetime = true,
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                return Task.CompletedTask;
            },
            OnTokenValidated = _ =>
            {
                return Task.CompletedTask;
            }
        };
    }
);
//JWT
var supabaseKey = builder.Configuration["Supabase:AnonKey"];

builder.Services.AddSingleton(provider =>
{
    var client = new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = false
    });

    // Inicializar el cliente (necesario para que Postgrest funcione)
    client.InitializeAsync().GetAwaiter().GetResult();

    return client;
});

var app = builder.Build();

// Pipeline
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();