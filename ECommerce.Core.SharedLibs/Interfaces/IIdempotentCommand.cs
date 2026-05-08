namespace ECommerce.Core.SharedLibs.Interfaces;

public interface IIdempotentCommand
{
    string IdempotencyKey { get; }
}
