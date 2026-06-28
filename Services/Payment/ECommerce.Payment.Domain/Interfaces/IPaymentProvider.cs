using ECommerce.Payment.Domain.Models;

namespace ECommerce.Payment.Domain.Interfaces;

public interface IPaymentGateway
{
    string ProviderName { get; }

    Task<PaymentProviderResult> CreatePaymentSessionAsync(
        PaymentProviderRequest request,
        CancellationToken cancellationToken = default);
}