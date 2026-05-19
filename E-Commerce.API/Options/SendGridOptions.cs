using System.ComponentModel.DataAnnotations;

namespace E_Commerce.API.Options;

public class SendGridOptions
{
    public const string SectionName = "SendGrid";

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;
}
