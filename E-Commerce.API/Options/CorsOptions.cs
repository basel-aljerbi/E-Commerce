using System.ComponentModel.DataAnnotations;

namespace E_Commerce.API.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    [Required]
    public string[] AllowedOrigins { get; set; } = [];
}
