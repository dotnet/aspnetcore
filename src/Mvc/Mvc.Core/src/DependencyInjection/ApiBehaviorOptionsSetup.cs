// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class ApiBehaviorOptionsSetup : IConfigureOptions<ApiBehaviorOptions>
{
    private ProblemDetailsFactory? _problemDetailsFactory;

    public void Configure(ApiBehaviorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.InvalidModelStateResponseFactory = context =>
        {
            // ProblemDetailsFactory depends on the ApiBehaviorOptions instance. We intentionally avoid constructor injecting
            // it in this options setup to to avoid a DI cycle.
            _problemDetailsFactory ??= context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            return ProblemDetailsInvalidModelStateResponse(_problemDetailsFactory, context);
        };

        ConfigureClientErrorMapping(options);
    }

    internal static IActionResult ProblemDetailsInvalidModelStateResponse(ProblemDetailsFactory problemDetailsFactory, ActionContext context)
    {
        var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
        ObjectResult result;
        if (problemDetails.Status == 400)
        {
            // For compatibility with 2.x, continue producing BadRequestObjectResult instances if the status code is 400.
            result = new BadRequestObjectResult(problemDetails);
        }
        else
        {
            result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status,
            };
        }
        result.ContentTypes.Add("application/problem+json");
        result.ContentTypes.Add("application/problem+xml");

        return result;
    }

    // Internal for unit testing
    internal static void ConfigureClientErrorMapping(ApiBehaviorOptions options)
    {
        foreach (var (statusCode, value) in ProblemDetailsDefaults.Defaults)
        {
            options.ClientErrorMapping[statusCode] = new()
            {
                Link = value.Type,
                Title = value.Title,
            };
        }
    }
}
