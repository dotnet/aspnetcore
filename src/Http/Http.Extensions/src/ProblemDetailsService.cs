// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal sealed class ProblemDetailsService : IProblemDetailsService
{
    private readonly IEnumerable<IProblemDetailsWriter> _writers;

    public ProblemDetailsService(
        IEnumerable<IProblemDetailsWriter> writers)
    {
        _writers = writers;
    }

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.ProblemDetails);
        ArgumentNullException.ThrowIfNull(context.HttpContext);

        if (context.HttpContext.Response.HasStarted || context.HttpContext.Response.StatusCode < 400)
        {
            return;
        }

        foreach (var writer in _writers)
        {
            if (await writer.TryWriteAsync(context))
            {
                break;
            }
        }
    }
}
