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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="isRouting"></param>
    /// <returns></returns>
    public bool IsEnabled(int statusCode, bool isRouting = false)
    {
        if (AllowedMapping == MappingOptions.Unspecified)
        {
            return false;
        }

        return isRouting ?
            AllowedMapping.HasFlag(MappingOptions.RoutingFailures) :
            statusCode switch
            {
                >= 400 and <= 499 => AllowedMapping.HasFlag(MappingOptions.ClientErrors),
                >= 500 => AllowedMapping.HasFlag(MappingOptions.Exceptions),
                _ => false,
            };
    }
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
