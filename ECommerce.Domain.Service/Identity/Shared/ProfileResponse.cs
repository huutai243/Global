namespace ECommerce.Domain.Service.Identity.Shared
{
    public sealed record ProfileResponse(
        Guid UserId, 
        Guid? CustomerId, 
        string Email, 
        string Role, 
        string FullName);
}
