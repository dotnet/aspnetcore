// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class ProblemDetailsClientErrorFactory : IClientErrorFactory
{
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly ProblemDetailsOptions _options;

    public ProblemDetailsClientErrorFactory(
        ProblemDetailsFactory problemDetailsFactory,
        IOptions<ProblemDetailsOptions> options)
    {
        _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IActionResult? GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        if (!_options.IsEnabled(clientError.StatusCode!.Value))
        {
            return null;
        }

        var problemDetails = _problemDetailsFactory.CreateProblemDetails(actionContext.HttpContext, clientError.StatusCode);

        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status,
            ContentTypes =
                {
                    "application/problem+json",
                    "application/problem+xml",
                },
        };
    }
}
