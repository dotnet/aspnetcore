// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ProblemDetailsClientErrorFactory : IClientErrorFactory
{
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly IProblemDetailsProvider? _problemDetailsProvider;

    public ProblemDetailsClientErrorFactory(
        ProblemDetailsFactory problemDetailsFactory,
        IProblemDetailsProvider? problemDetailsProvider = null)
    {
        _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        _problemDetailsProvider = problemDetailsProvider;
    }

    public IActionResult? GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        if (_problemDetailsProvider != null &&
           !_problemDetailsProvider.IsEnabled(clientError.StatusCode ?? 500))
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
