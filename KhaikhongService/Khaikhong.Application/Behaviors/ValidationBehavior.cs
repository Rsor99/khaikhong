using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Khaikhong.Application.Common.Models;
using MediatR;

namespace Khaikhong.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        ValidationContext<TRequest> context = new(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        List<ValidationFailure> failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        return CreateFailureResponse(failures);
    }

    private static TResponse CreateFailureResponse(IEnumerable<ValidationFailure> failures)
    {
        ValidationError[] errors = failures
            .Select(failure => new ValidationError(failure.PropertyName, failure.ErrorMessage))
            .ToArray();

        object payload = new { errors };

        Type responseType = typeof(TResponse);

        if (responseType == typeof(ApiResponse<object>))
        {
            return (TResponse)(object)ApiResponse<object>.Fail(
                status: 400,
                message: "Validation failed",
                data: payload);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            Type dataType = responseType.GenericTypeArguments[0];
            Type apiResponseType = typeof(ApiResponse<>).MakeGenericType(dataType);

            MethodInfo? failMethod = apiResponseType.GetMethod(
                nameof(ApiResponse<object>.Fail),
                [typeof(int), typeof(string), typeof(object)]);

            if (failMethod is not null)
            {
                object? failureResponse = failMethod.Invoke(
                    obj: null,
                    parameters: [400, "Validation failed", payload]);

                if (failureResponse is TResponse typedResponse)
                {
                    return typedResponse;
                }
            }
        }

        throw new InvalidOperationException(
            $"ValidationBehavior requires MediatR handlers to return ApiResponse<T>. Actual type: '{responseType}'.");
    }

    private sealed record ValidationError(string Field, string Message);
}
