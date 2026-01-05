using AuthAPI.Data;
using AuthAPI.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 1. SWAGGER WITH AUTH (Consitent with ProductAPI)
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] {} }
    });
});

// 2. JWT AUTHENTICATION SETUP
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 1. STOP AUTOMATIC MAPPING (Crucial for clean tokens)
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

            // 2. TELL .NET WHERE TO FIND ROLES and NAMES
            RoleClaimType = "role",
            NameClaimType = "username"
        };
    });

// 3. REGISTER DAPPER SERVICES
builder.Services.AddSingleton<DapperContext>();        // Manages Connection String
builder.Services.AddScoped<IUserRepository, UserRepository>(); // Handles SQL Logic

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// 4. ENABLE AUTH MIDDLEWARE
app.UseAuthentication(); // <--- Must be before Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();