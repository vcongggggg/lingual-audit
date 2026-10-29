using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lingual.Infrastructure.Validator;

public static class Extension
{
    public static OptionsBuilder<TOption> ValidateFluentValidation<TOption>(this OptionsBuilder<TOption> builder)
        where TOption : class
    {
        builder.Services.AddSingleton<IValidateOptions<TOption>>(service => new OptionValidation<TOption>(service));
        return builder;
    }

    public static async Task HandleValidationAsync<TRequest>(this IValidator<TRequest> validator, TRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        var failures = validationResult.Errors;
        if (failures.Count != 0) throw new ValidationException(failures);
    }

    [DebuggerStepThrough]
    public static IServiceCollection AddValidator(this IServiceCollection services, params System.Reflection.Assembly[] additionalAssemblies)
    {
        var assemblies = new List<System.Reflection.Assembly> { AssemblyReference.Assembly };
        if (additionalAssemblies is { Length: > 0 })
        {
            assemblies.AddRange(additionalAssemblies);
        }

        return services.Scan(scan => scan
            .FromAssemblies(assemblies.Distinct())
            .AddClasses(c => c.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
    }
}