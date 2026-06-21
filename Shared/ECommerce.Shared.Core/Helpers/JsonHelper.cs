using System.Text.Json;
using ECommerce.Shared.Core.Interfaces;

namespace ECommerce.Shared.Core.Helpers;

public sealed class JsonHelper : IJsonHelper
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public string Serialize<TValue>(TValue value)
    {
        return JsonSerializer.Serialize(value, JsonSerializerOptions);
    }

    public TValue? Deserialize<TValue>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TValue>(value, JsonSerializerOptions);
    }

    public TValue DeserializeRequired<TValue>(string value)
    {
        var result = Deserialize<TValue>(value);

        if (result is null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize JSON to type {typeof(TValue).Name}.");
        }

        return result;
    }

    public bool TryDeserialize<TValue>(string value, out TValue? result)
    {
        try
        {
            result = Deserialize<TValue>(value);
            return result is not null;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public object? Deserialize(string value, Type returnType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize(value, returnType, JsonSerializerOptions);
    }

    public TValue DeserializeRequired<TValue>(string value, string errorMessage)
    {
        var result = Deserialize<TValue>(value);

        if (result is null)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return result;
    }
}