// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Linq;

internal sealed class ProblemDetailsService : IProblemDetailsService
{
    private readonly IProblemDetailsWriter[] _writers;

    public ProblemDetailsService(
        IEnumerable<IProblemDetailsWriter> writers)
    {
        _writers = writers.ToArray();
    }

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        if (!await TryWriteAsync(context))
        {
            throw new InvalidOperationException("Unable to find a registered `IProblemDetailsWriter` that can write to the given context.");
        }
    }

    public async ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.ProblemDetails);
        ArgumentNullException.ThrowIfNull(context.HttpContext);

        // Try to write using all registered writers
        // sequentially and stop at the first one that
        // `canWrite`.
        for (var i = 0; i < _writers.Length; i++)
        {
            var selectedWriter = _writers[i];
            if (selectedWriter.CanWrite(context))
            {
                await selectedWriter.WriteAsync(context);
                return true;
            }
        }

        return false;
    }
}
