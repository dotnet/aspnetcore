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
    public MappingOptions AllowedMapping { get; set; } = MappingOptions.All;

    /// <summary>
    /// 
    /// </summary>
    public Action<HttpContext, ProblemDetails>? ConfigureDetails { get; set; }
}

/// <summary>
/// 
/// </summary>
[Flags]
public enum MappingOptions : uint
{
    /// <summary>
    /// 
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 
    /// </summary>
    Exceptions = 1,

    /// <summary>
    /// 404 / 405 / 415
    /// </summary>
    RoutingFailures = 2,

    /// <summary>
    /// 
    /// </summary>
    ClientErrors = 4,

    /// <summary>
    /// 
    /// </summary>
    All = RoutingFailures | Exceptions | ClientErrors,
}
