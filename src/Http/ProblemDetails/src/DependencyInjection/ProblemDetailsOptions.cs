// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 
/// </summary>
public class ProblemDetailsOptions
{
    /// <summary>
    /// 
    /// </summary>
    public ProblemDetailsTypes AllowedProblemTypes { get; set; } = ProblemDetailsTypes.All;

    /// <summary>
    /// 
    /// </summary>
    public Action<HttpContext, ProblemDetails>? ConfigureDetails { get; set; }
}
