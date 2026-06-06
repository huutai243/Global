namespace ECommerce.Shared.Core.Exceptions;

public class ConcurrencyException(string message) : Exception(message);
