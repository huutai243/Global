namespace ECommerce.Shared.Core.Exceptions;

public class IdempotencyConflictException(string message) : Exception(message);
