// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Used to send reset messages for protocols that support them such as HTTP/2 or HTTP/3.
/// </summary>
public interface IHttpResetFeature
{
    /// <summary>
    /// Send a reset message with the given error code. The request will be considered aborted.
    /// </summary>
    /// <param name="errorCode">The error code to send in the reset message.</param>
    void Reset(int errorCode);
}
