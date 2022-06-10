// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 
/// </summary>
public interface IProblemDetailsWriter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <param name="statusCode"></param>
    /// <param name="isRouting"></param>
    /// <returns></returns>
    bool CanWrite(HttpContext context, EndpointMetadataCollection? metadata, bool isRouting);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <param name="isRouting"></param>
    /// <param name="statusCode"></param>
    /// <param name="title"></param>
    /// <param name="type"></param>
    /// <param name="detail"></param>
    /// <param name="instance"></param>
    /// <param name="extensions"></param>
    /// <param name="configureDetails"></param>
    /// <returns></returns>
    Task WriteAsync(
        HttpContext context,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null,
        Action<HttpContext, ProblemDetails>? configureDetails = null);
}
