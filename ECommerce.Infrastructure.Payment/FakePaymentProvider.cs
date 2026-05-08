using ECommerce.Payment.Core.Interfaces;
using ECommerce.Payment.Core.Models;

namespace ECommerce.Infrastructure.Payment;

public sealed class FakePaymentProvider : IPaymentProvider
{
    public Task<PaymentProviderResult> PayAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default)
    {
        var result = request.Amount > 0
            ? new PaymentProviderResult(true, $"fake-{Guid.NewGuid():N}", null)
            : new PaymentProviderResult(false, null, "Amount must be greater than zero.");

        return Task.FromResult(result);
    }
}
