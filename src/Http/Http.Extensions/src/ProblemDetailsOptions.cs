// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Options for controlling the behavior of <see cref="IProblemDetailsService.WriteAsync(ProblemDetailsContext)"/>
/// and similar methods.
/// </summary>
public class ProblemDetailsOptions
{
    /// <summary>
    /// The operation that customizes the current <see cref="Mvc.ProblemDetails"/> instance.
    /// </summary>
    public Action<ProblemDetailsContext>? CustomizeProblemDetails { get; set; }
}
