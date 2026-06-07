namespace ECommerce.Shared.Messaging;

public interface IMessageNameResolver
{
    string ResolveMessageName(Type messageType);
}