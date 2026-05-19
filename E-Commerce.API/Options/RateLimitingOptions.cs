using System.ComponentModel.DataAnnotations;

namespace E_Commerce.API.Options;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 100;

    [Range(1, 3600)]
    public int WindowInSeconds { get; set; } = 60;

    [Range(1, 100)]
    public int AuthPermitLimit { get; set; } = 5;

    [Range(1, 60)]
    public int AuthWindowInMinutes { get; set; } = 15;
}
