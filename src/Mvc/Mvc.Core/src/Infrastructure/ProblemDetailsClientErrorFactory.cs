// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ProblemDetailsClientErrorFactory : IClientErrorFactory
{
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly ProblemDetailsMapper? _matcher;

    public ProblemDetailsClientErrorFactory(
        ProblemDetailsFactory problemDetailsFactory,
        ProblemDetailsMapper? matcher = null)
    {
        _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        _matcher = matcher;
    }

    public IActionResult? GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        if (_matcher != null &&
           !_matcher.CanMap(actionContext.HttpContext, statusCode: clientError.StatusCode))
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
