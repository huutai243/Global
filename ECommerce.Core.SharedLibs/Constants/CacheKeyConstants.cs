namespace ECommerce.Core.SharedLibs.Constants;

public static class CacheKeyConstants
{
    public static string ProductById(Guid productId)
    {
        return $"product:{productId}";
    }

    public static string CategoryById(Guid categoryId)
    {
        return $"category:{categoryId}";
    }

    public static string PublicProducts()
    {
        return "products:public";
    }

    public static string PublicProductsVersion()
    {
        return "products:public:version";
    }
}