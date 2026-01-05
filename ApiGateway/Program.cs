using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        b => b.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Load ocelot.json configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Ocelot Services
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowReact");
// Enable Ocelot Middleware
await app.UseOcelot();

app.Run();