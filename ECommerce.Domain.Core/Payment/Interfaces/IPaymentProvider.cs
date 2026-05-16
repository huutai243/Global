using ECommerce.Domain.Core.Payment.Models;

namespace ECommerce.Domain.Core.Payment.Interfaces;

public interface IPaymentProvider
{
    Task<PaymentProviderResult> PayAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default);
}
