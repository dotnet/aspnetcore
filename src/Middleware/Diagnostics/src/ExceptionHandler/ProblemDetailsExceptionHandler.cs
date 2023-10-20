// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics;

internal sealed class ProblemDetailsExceptionHandler(IProblemDetailsService? problemDetailsService = null) : IExceptionHandler
{
    private readonly IProblemDetailsService? _problemDetailsService = problemDetailsService;

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (_problemDetailsService == null)
        {
            return default;
        }

        return _problemDetailsService.TryWriteAsync(new()
        {
            HttpContext = httpContext,
            AdditionalMetadata = httpContext.GetEndpoint()?.Metadata,
            ProblemDetails = { Status = StatusCodes.Status500InternalServerError },
            Exception = exception,
        });
    }
}
