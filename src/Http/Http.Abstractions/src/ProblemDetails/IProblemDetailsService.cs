// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public interface IProblemDetailsService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="currentMetadata"></param>
    /// <param name="isRouting"></param>
    /// <param name="statusCode"></param>
    /// <param name="title"></param>
    /// <param name="type"></param>
    /// <param name="detail"></param>
    /// <param name="instance"></param>
    /// <param name="extensions"></param>
    /// <returns></returns>
    Task WriteAsync(
        HttpContext context,
        EndpointMetadataCollection? currentMetadata = null,
        bool isRouting = false,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="isRouting"></param>
    /// <returns></returns>
    bool IsEnabled(int statusCode, bool isRouting = false);
}
