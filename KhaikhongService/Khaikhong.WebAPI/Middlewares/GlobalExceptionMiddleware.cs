using System.Net.Mime;
using System.Text.Json;
using Khaikhong.Application.Common.Models;

namespace Khaikhong.WebAPI.Middlewares;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception encountered while processing the request.");
            await WriteErrorResponseAsync(context);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        ApiResponse<object> response = ApiResponse<object>.Fail(
            status: StatusCodes.Status500InternalServerError,
            message: "An unexpected error occurred",
            errors: new { message = "Something went wrong." });

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
