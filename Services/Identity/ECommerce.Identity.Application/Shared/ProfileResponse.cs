namespace ECommerce.Identity.Application.Shared
{
    public sealed record ProfileResponse(
        Guid UserId, 
        Guid? CustomerId, 
        string Email, 
        string Role, 
        string FullName);
}
