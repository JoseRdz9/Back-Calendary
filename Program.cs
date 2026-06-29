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
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);


// ================= CONFIG =================

var configuration = builder.Configuration;

var supabaseUrl = configuration["Supabase:Url"];
var supabaseKey = configuration["Supabase:AnonKey"];


if (string.IsNullOrEmpty(supabaseUrl))
    throw new Exception("Supabase URL missing");

if (string.IsNullOrEmpty(supabaseKey))
    throw new Exception("Supabase Key missing");


// ================= CONTROLLERS =================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


// ================= REPOSITORIES =================

builder.Services.AddScoped<ICycleRepository, CycleRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IPredictionRepository, PredictionRepository>();

builder.Services.AddScoped<IProfileService, ProfileService>();


// ================= SERVICES =================

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<JwtHelper>();

builder.Services.AddScoped<ICycleService, CycleService>();

builder.Services.AddScoped<ILogService, LogService>();

builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

builder.Services.AddSingleton<IPredictionEngine, CyclePredictionEngine>();

builder.Services.AddHttpClient();


// ================= SUPABASE CLIENT =================

builder.Services.AddSingleton<Supabase.Client>(_ =>
{

    var client = new Supabase.Client(
        supabaseUrl,
        supabaseKey,
        new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = false
        });


    client.InitializeAsync()
        .GetAwaiter()
        .GetResult();


    return client;

});



// ================= CONFIG OBJECTS =================

builder.Services.Configure<SupabaseSettings>(
    configuration.GetSection("Supabase")
);



// ================= DATABASE =================

builder.Services.AddDbContext<AppDbContext>(options =>
{

    options.UseNpgsql(
        configuration.GetConnectionString("DefaultConnection")
    );

});



// ================= CORS =================

builder.Services.AddCors(options =>
{

    options.AddPolicy("AllowAll", policy =>
    {

        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();

    });

});




// ================= AUTH SUPABASE JWT =================

var jwksUrl =
    $"{supabaseUrl}/auth/v1/.well-known/jwks.json";


var http = new HttpClient();


var jwksJson =
    await http.GetStringAsync(jwksUrl);


var keys =
    new JsonWebKeySet(jwksJson)
        .GetSigningKeys();

builder.Services
.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)

.AddJwtBearer(options =>
{


    options.TokenValidationParameters =
        new TokenValidationParameters
        {

            ValidateIssuerSigningKey = true,


            IssuerSigningKeys = keys,


            ValidateIssuer = true,


            ValidIssuer =
                $"{supabaseUrl}/auth/v1",



            ValidateAudience = true,


            ValidAudience =
                "authenticated",



            ValidateLifetime = true,



            NameClaimType =
                "sub"

        };



    options.Events =
        new JwtBearerEvents
        {


            OnAuthenticationFailed = context =>
            {


                Console.WriteLine(
                    context.Exception.Message
                );


                return Task.CompletedTask;

            },



            OnTokenValidated = context =>
            {
                return Task.CompletedTask;

            }


        };


});




// ================= BUILD =================

var app = builder.Build();



// ================= PIPELINE =================

app.UseCors("AllowAll");


app.UseSwagger();

app.UseSwaggerUI();


app.UseStaticFiles();



app.UseAuthentication();


app.UseAuthorization();



app.MapControllers();



// ================= PORT =================

var port =
    Environment.GetEnvironmentVariable("PORT")
    ?? "5095";


builder.WebHost.UseUrls(
    $"http://0.0.0.0:{port}"
);



app.Run();