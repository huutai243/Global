namespace ECommerce.Core.SharedLibs.Exceptions;

public class ForbiddenAccessException(string message) : Exception(message);
