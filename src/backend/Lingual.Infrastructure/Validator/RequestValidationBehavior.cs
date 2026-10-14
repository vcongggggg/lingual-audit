using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lingual.Infrastructure.Validator;

[DebuggerStepThrough]
public sealed class RequestValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<RequestValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validatorList = validators.ToList();
        if (validatorList.Count != 0)
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                validatorList.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                logger.LogWarning("[{Behavior}] Validation failed for {RequestName} with {ErrorCount} error(s)",
                    nameof(RequestValidationBehavior<TRequest, TResponse>), typeof(TRequest).Name, failures.Count);
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}