// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ProblemDetailsClientErrorFactory : IClientErrorFactory
{
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly ProblemDetailsOptions _options;

    public ProblemDetailsClientErrorFactory(
        ProblemDetailsFactory problemDetailsFactory,
        IOptions<ProblemDetailsOptions> options)
    {
        _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        _options = options.Value;
    }

    public IActionResult? GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        var statusCode = clientError.StatusCode ?? 500;
        var problemType = statusCode >= 500 ? ProblemDetailsTypes.Server : ProblemDetailsTypes.Client;

        if ((_options.AllowedProblemTypes & problemType) == ProblemDetailsTypes.None)
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
