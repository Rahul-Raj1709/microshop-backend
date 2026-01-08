using Confluent.Kafka;
using ConsumerWorker.Repositories;
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

        // Subscribe to BOTH topics
        var orderTopic = _config["Kafka:Topic"] ?? "order-requests";
        consumer.Subscribe(new[] { orderTopic, "review-events" });

        _logger.LogInformation("Consumer Started. Listening for Orders and Reviews...");

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
                        var topic = result.Topic;

                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var repo = scope.ServiceProvider.GetRequiredService<OrderRepository>();

                            // --- HANDLE ORDERS ---
                            if (topic == orderTopic)
                            {
                                var order = JsonSerializer.Deserialize<OrderRequest>(message);
                                if (order != null)
                                {
                                    _logger.LogInformation($"Processing Order for Product: {order.ProductId}");

                                    var product = await repo.GetProductInfoAsync(order.ProductId);
                                    if (product != null && product.Stock >= order.Quantity)
                                    {
                                        await repo.FinalizeOrderAsync(
                                            order.UserId, order.ProductId, product.Name,
                                            order.Quantity, product.Version, product.SellerId,
                                            product.Price, order.ShippingAddress ?? "N/A");
                                    }
                                }
                            }
                            // --- HANDLE REVIEWS ---
                            else if (topic == "review-events")
                            {
                                var review = JsonSerializer.Deserialize<ReviewEvent>(message);
                                if (review != null)
                                {
                                    _logger.LogInformation($"⭐ New Review for Product {review.ProductId} ({review.Rating} stars)");

                                    // Update the stats in Products table
                                    await repo.UpdateProductReviewStats(review.ProductId, review.Rating);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing message: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}

// Support Classes
public class OrderRequest
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string ShippingAddress { get; set; }
}

public class ReviewEvent
{
    public string Type { get; set; }
    public int ProductId { get; set; }
    public int Rating { get; set; }
}