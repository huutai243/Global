namespace ECommerce.Shared.Core.Exceptions;

public class ForbiddenAccessException(string message) : Exception(message);
