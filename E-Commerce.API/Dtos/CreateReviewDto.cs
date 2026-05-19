namespace E_Commerce.API.Dtos;

public record CreateReviewDto(
    int Rating,
    string? Comment
);
