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
    public MappingOptions Mapping { get; set; } = MappingOptions.ClientErrors | MappingOptions.Exceptions;
    public Action<HttpContext, ProblemDetails>? ConfigureDetails { get; set; }

    public bool IsEnabled(int statusCode, bool isRouting = false)
        => isRouting ? Mapping.HasFlag(MappingOptions.Routing) : statusCode switch
        {
            >= 400 and <= 499 => Mapping.HasFlag(MappingOptions.ClientErrors),
            >= 500 => Mapping.HasFlag(MappingOptions.Exceptions),
            _ => false,
        };
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
    None = 0,

    /// <summary>
    /// 
    /// </summary>
    ClientErrors = 1,

    /// <summary>
    /// 
    /// </summary>
    Routing = 2,

    /// <summary>
    /// 
    /// </summary>
    Exceptions = 4,

    /// <summary>
    /// 
    /// </summary>
    All = ClientErrors | Routing | Exceptions
}
