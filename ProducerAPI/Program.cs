using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using ProducerAPI; // For KafkaTopicHelper
using ProducerAPI.Data;          // <-- Add
using ProducerAPI.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<IOrderReadRepository, OrderReadRepository>();
// -----------------------
// 1. CONFIGURE SWAGGER FOR JWT (Adds the padlock button)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProducerAPI", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// 2. CONFIGURE AUTHENTICATION
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 1. DISABLE MAPPING
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])),

            // 2. CONFIGURE ROLE MAPPING
            RoleClaimType = "role",
            NameClaimType = "username"
        };
    }); 

var app = builder.Build();

// Kafka Topic Setup (Keep existing logic)
var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"];
var topicName = builder.Configuration["Kafka:Topic"];
if (!string.IsNullOrEmpty(bootstrapServers) && !string.IsNullOrEmpty(topicName))
{
    // Wrap in try-catch to prevent crash if Kafka isn't ready immediately
    try { await KafkaTopicHelper.EnsureTopicExists(bootstrapServers, topicName); }
    catch { /* Log error in real app */ }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// 3. ENABLE AUTH MIDDLEWARE (Must be in this order!)
app.UseAuthentication(); // <--- Verifies the "Who"
app.UseAuthorization();  // <--- Verifies the "Can they?"

app.MapControllers();

app.Run();