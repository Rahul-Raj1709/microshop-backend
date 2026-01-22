using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using ProductAPI.Data;          // For DapperContext
using ProductAPI.Repositories;  // For IProductRepository
using ProductAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 1. SWAGGER WITH AUTH
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

// 2. JWT AUTH
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

// 3. REGISTER DAPPER SERVICES (Replaces EF Core AddDbContext)
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddSingleton<ElasticSearchService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();

var app = builder.Build();

// Note: Removed the DB Migration/EnsureCreated block because you are managing the SQL Schema manually.

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication(); // Verify Identity
app.UseAuthorization();  // Verify Permissions (Roles)

app.MapControllers();

app.Run();