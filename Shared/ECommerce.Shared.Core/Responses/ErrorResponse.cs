namespace ECommerce.Shared.Core.Responses;

public sealed class ErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Details { get; set; } = Array.Empty<string>();
}
