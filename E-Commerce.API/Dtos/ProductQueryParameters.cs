using System.ComponentModel.DataAnnotations;

namespace E_Commerce.API.Dtos;

public class ProductQueryParameters
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    public int? CategoryId { get; set; }
    public string? Search { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string? SortBy { get; set; }

    public string SortOrder { get; set; } = "asc";
}
