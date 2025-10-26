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
        Type responseType = typeof(TResponse);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            object errorPayload = failures
                .Select(failure => new ValidationError(failure.PropertyName, failure.ErrorMessage))
                .ToArray();

            Type dataType = responseType.GenericTypeArguments[0];
            MethodInfo factory = typeof(ValidationBehavior<TRequest, TResponse>)
                .GetMethod(nameof(CreateFailureResponseInternal), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(dataType);

            object result = factory.Invoke(null, [errorPayload])!;
            return (TResponse)result;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior requires MediatR handlers to return ApiResponse<T>. Actual type: '{responseType}'.");
    }

    private static ApiResponse<TData> CreateFailureResponseInternal<TData>(object errors) =>
        ApiResponse<TData>.Fail(status: 400, message: "Validation failed", errors: errors);

    private sealed record ValidationError(string Field, string Error);
}
