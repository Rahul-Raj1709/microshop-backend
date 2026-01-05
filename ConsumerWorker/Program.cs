using ConsumerWorker;
using ConsumerWorker.Data;
using ConsumerWorker.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// 1. Register Dapper Services
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<OrderRepository>();

// 2. Register HTTP Client for PaymentAPI with RESILIENCE
builder.Services.AddHttpClient("PaymentClient", client =>
{
    // Docker internal networking
    client.BaseAddress = new Uri("http://payment-api:8080/");
})
.AddStandardResilienceHandler(); // <--- THIS ADDS RETRY & CIRCUIT BREAKER MAGIC

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();