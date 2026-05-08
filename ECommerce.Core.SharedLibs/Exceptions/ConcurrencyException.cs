namespace ECommerce.Core.SharedLibs.Exceptions;

public class ConcurrencyException(string message) : Exception(message);
