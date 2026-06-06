using System.ComponentModel.DataAnnotations;

namespace ECommerce.Cart.WebApi.Controllers;

public sealed class UpdateCartItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }
}