using System.ComponentModel.DataAnnotations;

namespace E_Commerce.API.Options;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; set; } = string.Empty;

    public string PublishableKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string WebhookSecret { get; set; } = string.Empty;
}
