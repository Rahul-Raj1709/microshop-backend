using Confluent.Kafka;
using ConsumerWorker.Repositories;
using System.Net.Http.Json; // Required for PostAsJsonAsync
using System.Text.Json;

namespace ConsumerWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(ILogger<Worker> logger, IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _config = config;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = "order-processor-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_config["Kafka:Topic"] ?? "order-requests");

        _logger.LogInformation("Consumer Started. Waiting for orders...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result != null)
                    {
                        var message = result.Message.Value;
                        _logger.LogInformation($"Kafka Message Received: {message}");

                        // 1. DESERIALIZE HERE (This was likely missing)
                        var order = JsonSerializer.Deserialize<OrderRequest>(message);

                        if (order != null)
                        {
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var repo = scope.ServiceProvider.GetRequiredService<OrderRepository>();

                                // CHANGED: Log ID instead of Name
                                _logger.LogInformation($"➡️ Processing ProductID: {order.ProductId} (x{order.Quantity})");

                                // 2. Get Product Info BY ID
                                var product = await repo.GetProductInfoAsync(order.ProductId);

                                if (product == null || product.Stock < order.Quantity)
                                {
                                    _logger.LogError($"❌ Stock Low or Product Missing (ID: {order.ProductId}).");
                                    continue;
                                }

                                // ... (Keep existing Payment and Finalize logic, passing product.Name if needed for logs) ...

                                // 3. Finalize Order
                                // Note: You might need to update FinalizeOrderAsync signature to accept ProductId
                                await repo.FinalizeOrderAsync(
                                    order.UserId,
                                    product.Name,
                                    order.Quantity,
                                    product.Version,
                                    product.SellerId, // <--- Pass SellerId
                                    product.Price     // <--- Pass Price
                                );
                            }
                        }
                    }
                }
                catch (ConsumeException e)
                {
                    _logger.LogError($"Kafka Error: {e.Error.Reason}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"General Error: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}

// Model Definition
public class OrderRequest
{
    public int UserId { get; set; }
    public int ProductId { get; set; } // <--- Renamed from OrderId
    public int Quantity { get; set; }
}