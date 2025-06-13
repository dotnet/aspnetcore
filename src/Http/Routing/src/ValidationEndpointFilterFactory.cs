#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
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
    private readonly record struct ValidatableParameterEntry(int Index, IValidatableInfo Parameter, string DisplayName);

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
                    GetDisplayName(parameters[i])));
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
                if (argument is null)
                {
                    continue;
                }

                var validationContext = new ValidationContext(argument, entry.DisplayName, context.HttpContext.RequestServices, items: null);

                if (validateContext == null)
                {
                    validateContext = new ValidateContext
                    {
                        ValidationOptions = options,
                        ValidationContext = validationContext
                    };
                }
                else
                {
                    validateContext.ValidationContext = validationContext;
                }

                await entry.Parameter.ValidateAsync(argument, validateContext, context.HttpContext.RequestAborted);
            }

            if (validateContext is { ValidationErrors.Count: > 0 })
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                var problemDetails = new HttpValidationProblemDetails(validateContext.ValidationErrors);

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
