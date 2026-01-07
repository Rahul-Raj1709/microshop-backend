using Dapper;
using System.Data;
using System.Text.Json;

namespace AuthAPI.TypeHandlers;

public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = JsonSerializer.Serialize(value);
        parameter.DbType = DbType.Object;
    }

    public override T Parse(object value)
    {
        if (value is string json)
        {
            return JsonSerializer.Deserialize<T>(json) ?? Activator.CreateInstance<T>();
        }
        return Activator.CreateInstance<T>();
    }
}