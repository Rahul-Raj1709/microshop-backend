using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ProductAPI.Models;

namespace ProductAPI.Services;

public class ElasticSearchService
{
    private readonly ElasticsearchClient _client;

    public ElasticSearchService(IConfiguration config)
    {
        var uri = new Uri(config["Elasticsearch:Uri"] ?? "http://localhost:9200");
        _client = new ElasticsearchClient(uri);
    }

    // 1. Index Single
    public async Task IndexProductAsync(Product product)
    {
        var response = await _client.IndexAsync(product, idx => idx.Index("products"));
        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Failed to index: {response.DebugInformation}");
        }
    }

    // NEW: Bulk Index (Fixes N+1 issue)
    public async Task BulkIndexProductsAsync(IEnumerable<Product> products)
    {
        var response = await _client.BulkAsync(b => b
            .Index("products")
            .IndexMany(products)
        );

        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Bulk index failed: {response.DebugInformation}");
        }
        else if (response.Errors)
        {
            foreach (var item in response.ItemsWithErrors)
            {
                Console.WriteLine($"Failed to index item {item.Id}: {item.Error}");
            }
        }
    }

    // 2. Delete
    public async Task DeleteProductAsync(int id)
    {
        await _client.DeleteAsync<Product>(id, idx => idx.Index("products"));
    }

    // 3. Search
    public async Task<IEnumerable<Product>> SearchAsync(string query, int? sellerId = null)
    {
        var response = await _client.SearchAsync<Product>(s => s
            .Index("products")
            .Query(q => q
                .Bool(b => {
                    // A. Must match keywords (Name, Desc, Category)
                    b.Must(m => m
                        .MultiMatch(mm => mm
                            .Fields(new[] { "name", "description", "category" })
                            .Query(query)
                            .Fuzziness(new Fuzziness("AUTO"))
                        )
                    );

                    // B. Optional Filter (Only for Admins)
                    if (sellerId.HasValue)
                    {
                        b.Filter(f => f.Term(t => t.Field(p => p.seller_id).Value(sellerId.Value)));
                    }
                })
            )
        );

        return response.Documents;
    }
}