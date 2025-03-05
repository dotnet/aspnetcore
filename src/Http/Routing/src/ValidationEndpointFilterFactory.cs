// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Validation;

internal static class ValidationEndpointFilterFactory
{
    private const string ValidationContextJustification = "The DisplayName property is always statically initialized in the ValidationContext through this codepath.";

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = ValidationContextJustification)]
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters();
        var options = context.ApplicationServices.GetService<IOptions<ValidationOptions>>()?.Value;
        if (options is null)
        {
            return next;
        }

        var parameterCount = parameters.Length;
        var validatableParameters = new ValidatableParameterInfo[parameterCount];
        var hasValidatableParameters = false;

        for (var i = 0; i < parameterCount; i++)
        {
            if (options.TryGetValidatableParameterInfo(parameters[i], out var validatableParameter))
            {
                validatableParameters[i] = validatableParameter;
                hasValidatableParameters = true;
            }
        }

        if (!hasValidatableParameters)
        {
            return next;
        }

        var validatableContext = new ValidatableContext { ValidationOptions = options };
        return async (context) =>
        {
            validatableContext.ValidationErrors?.Clear();

            for (var i = 0; i < context.Arguments.Count; i++)
            {
                var validatableParameter = validatableParameters[i];

                var argument = context.Arguments[i];
                if (argument is null || validatableParameter is null)
                {
                    continue;
                }
                // ValidationContext is not trim-friendly in codepaths that don't
                // initialize an explicit DisplayName. We can suppress the warning here.
                // Eventually, this can be removed when the code is updated to
                // use https://github.com/dotnet/runtime/issues/113134.
                var validationContext = new ValidationContext(argument, context.HttpContext.RequestServices, items: null) { DisplayName = validatableParameter.DisplayName };
                validatableContext.ValidationContext = validationContext;
                await validatableParameter.Validate(argument, validatableContext);
            }

            if (validatableContext.ValidationErrors is { Count: > 0 })
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.HttpContext.Response.ContentType = "application/problem+json";
                return await ValueTask.FromResult(new HttpValidationProblemDetails(validatableContext.ValidationErrors));
            }

            return next;
        };
    }
}
