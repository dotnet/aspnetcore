// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ProblemDetailsClientErrorFactory : IClientErrorFactory
{
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly ProblemDetailsOptions? _options;
    private readonly IProblemDetailsService? _problemDetailsProvider;

    public ProblemDetailsClientErrorFactory(
        ProblemDetailsFactory problemDetailsFactory,
        IOptions<ProblemDetailsOptions>? options = null,
        IProblemDetailsService? problemDetailsProvider = null)
    {
        _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        _options = options?.Value;
        _problemDetailsProvider = problemDetailsProvider;
    }

    public IActionResult? GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        if (_problemDetailsProvider != null &&
           !_options?.IsEnabled(clientError.StatusCode ?? 500) == true)
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
