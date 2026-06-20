namespace ECommerce.Cart.Infrastructure.Client
{
    public sealed class CatalogClientOptions
    {
        public const string SectionName = "CatalogClient";

        public string BaseAddress { get; init; } = string.Empty;

        public string ProductSnapshotPath { get; init; } = string.Empty;
    }
}
