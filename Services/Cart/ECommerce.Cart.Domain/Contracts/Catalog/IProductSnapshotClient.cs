using ECommerce.Cart.Domain.Models;

namespace ECommerce.Cart.Domain.Contracts.Catalog
{
    public interface IProductSnapshotClient
    {
        Task<ProductSnapshot?> GetProductSnapshotAsync(
            Guid productId,
            CancellationToken cancellationToken);
    }
}
