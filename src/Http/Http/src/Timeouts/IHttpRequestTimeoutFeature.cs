// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Timeouts;

/// <summary>
/// 
/// </summary>
public interface IHttpRequestTimeoutFeature
{
    /// <summary>
    /// 
    /// </summary>
    CancellationToken RequestTimeoutToken { get; }

    /// <summary>
    /// 
    /// </summary>
    void DisableTimeout();
}
