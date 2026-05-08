using ECommerce.Payment.Core.Models;

namespace ECommerce.Payment.Core.Interfaces;

public interface IPaymentProvider
{
    Task<PaymentProviderResult> PayAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default);
}
