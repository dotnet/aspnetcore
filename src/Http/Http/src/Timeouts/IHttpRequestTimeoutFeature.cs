// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Timeouts;

/// <summary>
/// Used to control timeouts on the current request.
/// </summary>
public interface IHttpRequestTimeoutFeature
{
    /// <summary>
    /// A <see cref="CancellationToken" /> that will trigger when the request times out.
    /// </summary>
    CancellationToken RequestTimeoutToken { get; }

    /// <summary>
    /// Disables the request timeout if it hasn't already expired. This does not
    /// trigger the <see cref="RequestTimeoutToken"/>.
    /// </summary>
    void DisableTimeout();
}
