namespace ECommerce.Shared.Messaging;

public sealed class DefaultMessageNameResolver : IMessageNameResolver
{
    public string ResolveMessageName(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return messageType.FullName ?? messageType.Name;
    }
}