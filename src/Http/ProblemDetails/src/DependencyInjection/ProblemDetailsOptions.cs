// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Options for controlling the behavior of <see cref="IProblemDetailsService.WriteAsync(ProblemDetailsContext)"/>
/// and similar methods.
/// </summary>
public class ProblemDetailsOptions
{
    /// <summary>
    /// Controls the ProblemDetails types allowed when auto-generating the response payload.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="ProblemDetailsTypes.All"/>.
    /// </remarks>
    public ProblemDetailsTypes AllowedProblemTypes { get; set; } = ProblemDetailsTypes.All;

    /// <summary>
    /// The operation that configures the current <see cref="ProblemDetails"/> instance.
    /// </summary>
    public Action<HttpContext, ProblemDetails>? ConfigureDetails { get; set; }
}
