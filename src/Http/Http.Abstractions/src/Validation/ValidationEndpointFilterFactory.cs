// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Validation;

internal static class ValidationEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters();
        var validatableTypeInfoResolver = context.ApplicationServices.GetService<IValidatableInfoResolver>();
        if (validatableTypeInfoResolver is null)
        {
            return next;
        }
        var validatableParameters = parameters.Select(validatableTypeInfoResolver.GetValidatableParameterInfo);
        return async (context) =>
        {
            var validationErrors = new Dictionary<string, string[]>();

            for (var i = 0; i < context.Arguments.Count; i++)
            {
                var validatableParameter = validatableParameters.ElementAt(i);

                var argument = context.Arguments[i];
                if (argument is null || validatableParameter is null)
                {
                    continue;
                }
                await validatableParameter.Validate(argument, "", validationErrors, validatableTypeInfoResolver, context.HttpContext.RequestServices);
            }

            if (validationErrors.Count > 0)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.HttpContext.Response.ContentType = "application/problem+json";
                return await ValueTask.FromResult(new HttpValidationProblemDetails(validationErrors));
            }

            return await next(context);
        };
    }
}
