using System.ComponentModel.DataAnnotations;

namespace E_Commerce.API.Dtos;

public class PaymentRequestDto
{
    [Required]
    public int OrderId { get; set; }
}
