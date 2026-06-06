using ECommerce.Payment.Domain.Models;

namespace ECommerce.Payment.Domain.Interfaces;

public interface IPaymentProvider
{
    Task<PaymentProviderResult> PayAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default);
}
