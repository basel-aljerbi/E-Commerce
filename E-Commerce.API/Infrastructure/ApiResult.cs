using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.API.Infrastructure;

public static class ApiResult
{
    public static IResult Success(object? data = null, string? message = null)
    {
        return Results.Ok(new ApiResponse
        {
            Success = true,
            Data = data,
            Message = message
        });
    }

    public static IResult Created(string uri, object? data = null)
    {
        return Results.Created(uri, new ApiResponse
        {
            Success = true,
            Data = data
        });
    }

    public static IResult BadRequest(string message, IDictionary<string, string[]>? errors = null)
    {
        return Results.BadRequest(new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        });
    }

    public static IResult NotFound(string? message = null)
    {
        return Results.NotFound(new ApiResponse
        {
            Success = false,
            Message = message ?? "Resource not found"
        });
    }

    public static IResult Problem(string detail, int statusCode = 500)
    {
        return Results.Problem(
            detail: detail,
            statusCode: statusCode,
            title: statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                409 => "Conflict",
                _ => "Server Error"
            });
    }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Message { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
