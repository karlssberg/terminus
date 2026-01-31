using System.ComponentModel.DataAnnotations;

namespace Terminus.Interceptors.Validation;

/// <summary>
/// Intercepts facade method invocations to validate method parameters using DataAnnotations.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor validates all method parameters decorated with validation attributes
/// (e.g., <see cref="RequiredAttribute"/>, <see cref="RangeAttribute"/>, <see cref="StringLengthAttribute"/>).
/// </para>
/// <para>
/// If validation fails, a <see cref="ValidationException"/> is thrown with aggregated error messages.
/// Parameters without validation attributes are skipped.
/// </para>
/// </remarks>
public class ValidationInterceptor : FacadeInterceptor
{
    /// <summary>
    /// Intercepts synchronous facade method invocations (void or result methods).
    /// </summary>
    public override TResult Intercept<TResult>(
        FacadeInvocationContext context,
        FacadeInvocationDelegate<TResult> next)
    {
        ValidateArguments(context);
        return next();
    }

    /// <summary>
    /// Intercepts asynchronous facade method invocations (Task or Task&lt;T&gt; methods).
    /// </summary>
    public override async ValueTask<TResult> InterceptAsync<TResult>(
        FacadeInvocationContext context,
        FacadeAsyncInvocationDelegate<TResult> next)
    {
        ValidateArguments(context);
        return await next();
    }

    /// <summary>
    /// Intercepts streaming facade method invocations (IAsyncEnumerable&lt;T&gt; methods).
    /// </summary>
    public override async IAsyncEnumerable<TItem> InterceptStream<TItem>(
        FacadeInvocationContext context,
        FacadeStreamInvocationDelegate<TItem> next)
    {
        ValidateArguments(context);

        await foreach (var item in next())
        {
            yield return item;
        }
    }

    /// <summary>
    /// Validates all method arguments using DataAnnotations.
    /// </summary>
    private static void ValidateArguments(FacadeInvocationContext context)
    {
        if (context.Arguments.Length == 0)
        {
            return;
        }

        var parameters = context.Method.GetParameters();
        var validationErrors = new List<ValidationResult>();

        for (var i = 0; i < context.Arguments.Length; i++)
        {
            var argument = context.Arguments[i];
            if (argument == null)
            {
                continue;
            }

            var parameterName = i < parameters.Length ? parameters[i].Name ?? $"arg{i}" : $"arg{i}";
            var validationContext = new ValidationContext(argument)
            {
                MemberName = parameterName,
                DisplayName = parameterName
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(argument, validationContext, results, validateAllProperties: true);

            if (!isValid)
            {
                validationErrors.AddRange(results);
            }
        }

        if (validationErrors.Count > 0)
        {
            var errorMessage = string.Join(Environment.NewLine, validationErrors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Validation failed for method '{context.Method.Name}':{Environment.NewLine}{errorMessage}");
        }
    }
}
