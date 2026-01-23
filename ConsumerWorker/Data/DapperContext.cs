using Npgsql;
using System.Data;

namespace ConsumerWorker.Data;

public class DapperContext
{
    private readonly IConfiguration _configuration;
    private readonly string _catalogConnectionString;
    private readonly string _salesConnectionString;

    public DapperContext(IConfiguration configuration)
    {
        _configuration = configuration;
        // Fetch from config (matches docker-compose env variables)
        _catalogConnectionString = _configuration.GetConnectionString("CatalogConnection")
                                   ?? throw new ArgumentNullException("CatalogConnection not found");

        _salesConnectionString = _configuration.GetConnectionString("SalesConnection")
                                 ?? throw new ArgumentNullException("SalesConnection not found");
    }

    public IDbConnection CreateCatalogConnection() => new NpgsqlConnection(_catalogConnectionString);
    public IDbConnection CreateSalesConnection() => new NpgsqlConnection(_salesConnectionString);
}