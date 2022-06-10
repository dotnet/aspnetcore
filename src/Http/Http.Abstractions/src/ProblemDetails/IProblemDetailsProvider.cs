// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public interface IProblemDetailsProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="isRouting"></param>
    /// <returns></returns>
    bool IsEnabled(int statusCode, bool isRouting = false);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="currentMetadata"></param>
    /// <param name="isRouting"></param>
    /// <returns></returns>
    IProblemDetailsWriter? GetWriter(
        HttpContext context,
        EndpointMetadataCollection? currentMetadata = null,
        bool isRouting = false);
}
