namespace ECommerce.Core.SharedLibs.Exceptions;

public class IdempotencyConflictException(string message) : Exception(message);
