#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Http.Validation;

internal static class ValidationEndpointFilterFactory
{
    // A small struct to hold the validatable parameter details to avoid allocating arrays for parameters that don't need validation
    private readonly record struct ValidatableParameterEntry(int Index, IValidatableParameterInfo Parameter, string Name, Type ParameterType);

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters();
        var options = context.ApplicationServices.GetService<IOptions<ValidationOptions>>()?.Value;
        if (options is null || options.Resolvers.Count == 0)
        {
            return next;
        }
        var serviceProviderIsService = context.ApplicationServices.GetService<IServiceProviderIsService>();

        // Use a list to only store validatable parameters instead of arrays for all parameters
        List<ValidatableParameterEntry>? validatableParameters = null;

        for (var i = 0; i < parameters.Length; i++)
        {
            // Ignore parameters that are resolved from the DI container.
            if (IsServiceParameter(parameters[i], serviceProviderIsService))
            {
                continue;
            }

            if (options.TryGetValidatableParameterInfo(parameters[i], out var validatableParameter))
            {
                validatableParameters ??= [];
                validatableParameters.Add(new ValidatableParameterEntry(
                    i,
                    validatableParameter,
                    parameters[i].Name!,
                    parameters[i].ParameterType));
            }
        }

        if (validatableParameters is null || validatableParameters.Count == 0)
        {
            return next;
        }

        return async (context) =>
        {
            ValidateContext? validateContext = null;

            foreach (var entry in validatableParameters)
            {
                if (entry.Index >= context.Arguments.Count)
                {
                    break;
                }

                var argument = context.Arguments[entry.Index];
                // For nullable value types (int?, long?, etc.), we need to detect when no value was provided.
                // When model binding cannot bind a value for a nullable type, it produces default(T) = 0 (boxed),
                // NOT null. We must detect this "default value" case and treat it as null for validation.
                var isDefaultValue = IsNullableValueType(entry.ParameterType) && IsDefaultValue(argument, entry.ParameterType);

                // Skip validation only if argument is null AND parameter is NOT nullable.
                // "Nullable" means: nullable value types (int?, etc.) OR nullable reference types (string?, etc.)
                // Nullable value types should NOT be skipped even with null - they need validation attributes to run.
                // Non-nullable types with null argument are filtered out by model binding, so skip them.
                var isNullable = entry.ParameterType.IsClass || IsNullableValueType(entry.ParameterType);
                if (argument is null && !isNullable)
                {
                    continue;
                }

                // For nullable value types with null or default value, we pass null to validation
                // but still need a non-null placeholder for ValidationContext.
                // For reference types (including nullable reference types like string?), we need a non-null placeholder.
                object? validationInstance = argument;if ((argument is null || isDefaultValue) && IsNullableValueType(entry.ParameterType))

                {
                    // Use the default value as a placeholder for ValidationContext only
                    var underlyingType = Nullable.GetUnderlyingType(entry.ParameterType);
                    validationInstance = underlyingType != null ? CreateDefaultValue(underlyingType) : argument;
                }

                // ValidationContext requires a non-null instance.
                // For reference types with null, we use an empty string as placeholder.

                if (validationInstance is null)
                {
                    if (entry.ParameterType.IsClass)
                    {
                        validationInstance = CreateReferenceTypeInstance(entry.ParameterType);
                    }
                    else
                    {
                        validationInstance = string.Empty;
                    }
                }

                // ValidationContext.DisplayName is overwritten by ValidatableParameterInfo.ValidateAsync
                // once the localized display name is resolved; the parameter name acts as a placeholder.
                var validationContext = new ValidationContext(validationInstance, entry.Name, context.HttpContext.RequestServices, items: null);

                if (validateContext == null)
                {
                    validateContext = new ValidateContext
                    {
                        ValidationOptions = options,
                        ValidationContext = validationContext,
                    };
                }
                else
                {
                    validateContext.ValidationContext = validationContext;
                }

                // Pass the ACTUAL argument to ValidateAsync - for nullable value types with no value bound,
                // this will be null (not the default/0) so validation attributes can properly evaluate it.
                var valueToValidate = (argument is null || isDefaultValue) && IsNullableValueType(entry.ParameterType)
                    ? null
                    : argument;
                await entry.Parameter.ValidateAsync(valueToValidate, validateContext, context.HttpContext.RequestAborted);
            }

            if (validateContext is { ValidationErrors.Count: > 0 })
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                var problemDetails = new HttpValidationProblemDetails(validateContext.ValidationErrors)
                {
                    Status = StatusCodes.Status400BadRequest
                };

                var problemDetailsService = context.HttpContext.RequestServices.GetService<IProblemDetailsService>();
                if (problemDetailsService is not null)
                {
                    if (await problemDetailsService.TryWriteAsync(new()
                    {
                        HttpContext = context.HttpContext,
                        ProblemDetails = problemDetails
                    }))
                    {
                        // We need to prevent further execution, because the actual
                        // ProblemDetails response has already been written by ProblemDetailsService.
                        return EmptyHttpResult.Instance;
                    }
                }

                // Fallback to the default implementation.
                context.HttpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;
                return problemDetails;
            }

            return await next(context);
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067")]
    private static object CreateReferenceTypeInstance(Type type)
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
    }

    private static bool IsServiceParameter(ParameterInfo parameterInfo, IServiceProviderIsService? isService)
        => HasFromServicesAttribute(parameterInfo) ||
           (isService?.IsService(parameterInfo.ParameterType) == true);

    private static bool HasFromServicesAttribute(ParameterInfo parameterInfo)
        => parameterInfo.CustomAttributes.OfType<IFromServiceMetadata>().Any();

    private static bool IsNullableValueType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    // Detects if an argument represents "no value provided" for validation purposes.
    // For nullable value types: argument is null means no binding attempt was made.
    // We do NOT treat default(T) (like 0) as "no value" because ?id=0 explicitly provides 0.
    private static bool IsDefaultValue(object? argument, Type parameterType)
    {
        // If argument is null and parameter is nullable, treat as "no value provided"
        // This handles the case when no query param is given - argument is null
        if (argument is null)
        {
            return IsNullableValueType(parameterType);
        }

        // IMPORTANT: We do NOT check if non-null argument equals default(T).
        // Reason: ?id=0 explicitly provides 0, which should validate as 0, not as null.
        // Model binding gives us null when no binding attempt, not default(T).
        return false;
    }

    // We deliberately use reflection to create instances for ValidationContext's non-null requirement.
    // This is safe because we only call this for nullable value types (int?, long?, DateTime?, etc.)
    // whose underlying types are value types with zero-alloc default values.
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Safe for value types only")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Safe for value types only")]
    private static object CreateDefaultValue(Type type)
    {
        if (type == typeof(int))
        {
            return 0;
        }
        if (type == typeof(long))
        {
            return 0L;
        }
        if (type == typeof(double))
        {
            return 0.0;
        }
        if (type == typeof(float))
        {
            return 0f;
        }
        if (type == typeof(bool))
        {
            return false;
        }
        if (type == typeof(DateTime))
        {
            return DateTime.MinValue;
        }
        if (type == typeof(decimal))
        {
            return 0m;
        }
        if (type == typeof(Guid))
        {
            return Guid.Empty;
        }
        // Fallback for other value types - use GetUninitializedObject
        return System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
    }
}
