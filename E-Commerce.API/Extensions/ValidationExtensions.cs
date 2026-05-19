using FluentValidation;

namespace E_Commerce.API.Extensions;

public static class ValidationExtensions
{
    public static async Task<IResult?> ValidateAndReturnAsync<T>(
        this IValidator<T> validator,
        T model,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(model, cancellationToken);
        if (result.IsValid) return null;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
