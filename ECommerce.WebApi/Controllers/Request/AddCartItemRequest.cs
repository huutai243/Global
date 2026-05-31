using System.ComponentModel.DataAnnotations;

namespace ECommerce.WebApi.Controllers.Cart;

public sealed class AddCartItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }
}