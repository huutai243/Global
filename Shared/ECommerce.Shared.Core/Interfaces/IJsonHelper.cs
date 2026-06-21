namespace ECommerce.Shared.Core.Interfaces;

public interface IJsonHelper
{
    string Serialize<TValue>(TValue value);

    TValue? Deserialize<TValue>(string value);

    TValue DeserializeRequired<TValue>(string value);

    TValue DeserializeRequired<TValue>(string value, string errorMessage);

    bool TryDeserialize<TValue>(string value, out TValue? result);

    object? Deserialize(string value, Type returnType);
}