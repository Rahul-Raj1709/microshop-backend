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

    // 1. Index
    public async Task IndexProductAsync(Product product)
    {
        var response = await _client.IndexAsync(product, idx => idx.Index("products"));
        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Failed to index: {response.DebugInformation}");
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