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

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.ProblemDetails);
        ArgumentNullException.ThrowIfNull(context.HttpContext);

        if (context.HttpContext.Response.HasStarted ||
            context.HttpContext.Response.StatusCode < 400 ||
            _writers.Length == 0)
        {
            return ValueTask.CompletedTask;
        }

        IProblemDetailsWriter? selectedWriter = null;

        if (_writers.Length == 1)
        {
            selectedWriter = _writers[0];

            return selectedWriter.CanWrite(context) ?
                selectedWriter.WriteAsync(context) :
                ValueTask.CompletedTask;
        }

        for (var i = 0; i < _writers.Length; i++)
        {
            if (_writers[i].CanWrite(context))
            {
                selectedWriter = _writers[i];
                break;
            }
        }

        if (selectedWriter != null)
        {
            return selectedWriter.WriteAsync(context);
        }

        return ValueTask.CompletedTask;
    }
}
