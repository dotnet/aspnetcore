#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Validation;

internal static class ValidationEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters();
        var options = context.ApplicationServices.GetService<IOptions<ValidationOptions>>()?.Value;
        if (options is null || options.Resolvers.Count == 0)
        {
            return next;
        }

        var serviceProviderIsService = context.ApplicationServices.GetService<IServiceProviderIsService>();

        var parameterCount = parameters.Length;
        var validatableParameters = new IValidatableInfo[parameterCount];
        var parameterDisplayNames = new string[parameterCount];
        var hasValidatableParameters = false;

        for (var i = 0; i < parameterCount; i++)
        {
            // Ignore parameters that are resolved from the DI container.
            if (IsServiceParameter(parameters[i], serviceProviderIsService))
            {
                continue;
            }

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

        return async (context) =>
        {
            ValidateContext? validateContext = null;

            // Get JsonOptions from DI if available
            var jsonOptions = context.HttpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value;

            for (var i = 0; i < context.Arguments.Count; i++)
            {
                var validatableParameter = validatableParameters[i];
                var displayName = parameterDisplayNames[i];

                var argument = context.Arguments[i];
                if (argument is null || validatableParameter is null)
                {
                    continue;
                }

                var validationContext = new ValidationContext(argument, displayName, context.HttpContext.RequestServices, items: null);

                if (validateContext == null)
                {
                    validateContext = new ValidateContext
                    {
                        ValidationOptions = options,
                        ValidationContext = validationContext,
                        SerializerOptions = jsonOptions?.SerializerOptions
                    };
                }
                else
                {
                    validateContext.ValidationContext = validationContext;
                }

                await validatableParameter.ValidateAsync(argument, validateContext, context.HttpContext.RequestAborted);
            }

            if (validateContext is { ValidationErrors.Count: > 0 })
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.HttpContext.Response.ContentType = "application/problem+json";
                return await ValueTask.FromResult(new HttpValidationProblemDetails(validateContext.ValidationErrors));
            }

            return await next(context);
        };
    }

    private static bool IsServiceParameter(ParameterInfo parameterInfo, IServiceProviderIsService? isService)
        => HasFromServicesAttribute(parameterInfo) ||
           (isService?.IsService(parameterInfo.ParameterType) == true);

    private static bool HasFromServicesAttribute(ParameterInfo parameterInfo)
        => parameterInfo.CustomAttributes.OfType<IFromServiceMetadata>().Any();

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
