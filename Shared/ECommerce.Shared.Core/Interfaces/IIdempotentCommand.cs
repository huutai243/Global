namespace ECommerce.Shared.Core.Interfaces;

public interface IIdempotentCommand
{
    string IdempotencyKey { get; }
}
