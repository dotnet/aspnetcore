// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Validation;

internal static class ValidationEndpointFilterFactory
{
    private const string ValidationContextJustification = "The DisplayName property is always statically initialized in the ValidationContext through this codepath.";

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters();
        var options = context.ApplicationServices.GetService<IOptions<ValidationOptions>>()?.Value;
        if (options is null)
        {
            return next;
        }

        var parameterCount = parameters.Length;
        var validatableParameters = new IValidatableInfo[parameterCount];
        var parameterDisplayNames = new string[parameterCount];
        var hasValidatableParameters = false;

        for (var i = 0; i < parameterCount; i++)
        {
            if (options.TryGetValidatableParameterInfo(parameters[i], out var validatableParameter))
            {
                validatableParameters[i] = validatableParameter;
                parameterDisplayNames[i] = GetDisplayName(parameters[i]);
                hasValidatableParameters = true;
            }
        }

        if (!hasValidatableParameters)
        {
            return next;
        }

        var validatableContext = new ValidateContext { ValidationOptions = options };
        return async (context) =>
        {
            validatableContext.ValidationErrors?.Clear();

            for (var i = 0; i < context.Arguments.Count; i++)
            {
                var validatableParameter = validatableParameters[i];
                var displayName = parameterDisplayNames[i];

                var argument = context.Arguments[i];
                if (argument is null || validatableParameter is null)
                {
                    continue;
                }
                // ValidationContext is not trim-friendly in codepaths that don't
                // initialize an explicit DisplayName. We can suppress the warning here.
                // Eventually, this can be removed when the code is updated to
                // use https://github.com/dotnet/runtime/issues/113134.
                var validationContext = CreateValidationContext(argument, displayName, context.HttpContext.RequestServices);
                validatableContext.ValidationContext = validationContext;
                await validatableParameter.ValidateAsync(argument, validatableContext, context.HttpContext.RequestAborted);
            }

            if (validatableContext.ValidationErrors is { Count: > 0 })
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.HttpContext.Response.ContentType = "application/problem+json";
                return await ValueTask.FromResult(new HttpValidationProblemDetails(validatableContext.ValidationErrors));
            }

            return await next(context);
        };
    }

    /// <remarks>
    /// ValidationContext is not trim-friendly in codepaths that don't
    /// initialize an explicit DisplayName. We can suppress the warning here.
    /// Eventually, this can be removed when the code is updated to
    /// use https://github.com/dotnet/runtime/issues/113134.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = ValidationContextJustification)]
    private static ValidationContext CreateValidationContext(object argument, string displayName, IServiceProvider serviceProvider)
        => new(argument, serviceProvider, items: null) { DisplayName = displayName };

    private static string GetDisplayName(ParameterInfo parameterInfo)
    {
        var displayAttribute = parameterInfo.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute != null)
        {
            return displayAttribute.Name ?? parameterInfo.Name!;
        }

        return parameterInfo.Name!;
    }
}
