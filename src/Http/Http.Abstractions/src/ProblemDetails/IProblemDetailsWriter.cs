// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public interface IProblemDetailsWriter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    bool CanWrite(HttpContext context, EndpointMetadataCollection? additionalMetadata);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="statusCode"></param>
    /// <param name="title"></param>
    /// <param name="type"></param>
    /// <param name="detail"></param>
    /// <param name="instance"></param>
    /// <param name="extensions"></param>
    /// <returns></returns>
    Task WriteAsync(
        HttpContext context,
        int? statusCode,
        string? title,
        string? type,
        string? detail,
        string? instance,
        IDictionary<string, object?>? extensions);
}
