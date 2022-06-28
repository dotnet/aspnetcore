// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed partial class DefaultProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly ProblemDetailsOptions _options;

    public DefaultProblemDetailsWriter(IOptions<ProblemDetailsOptions> options)
    {
        _options = options.Value;
    }

    public async ValueTask<bool> WriteAsync(ProblemDetailsContext context)
    {
        var problemResult = TypedResults.Problem(context.ProblemDetails ?? new ProblemDetails());
        _options.ConfigureDetails?.Invoke(context.HttpContext, problemResult.ProblemDetails);

        await problemResult.ExecuteAsync(context.HttpContext);
        return context.HttpContext.Response.HasStarted;
    }
}
