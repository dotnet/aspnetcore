// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Used to indicate if the request can have a body.
/// </summary>
public interface IHttpRequestBodyDetectionFeature
{
    /// <summary>
    /// Indicates if the request can have a body.
    /// </summary>
    /// <remarks>
    /// This returns true when:
    /// <list type="bullet">
    /// <item><description>
    /// It's an HTTP/1.x request with a non-zero Content-Length or a 'Transfer-Encoding: chunked' header.
    /// </description></item>
    /// <item><description>
    /// It's an HTTP/2 request that did not set the END_STREAM flag on the initial headers frame.
    /// </description></item>
    /// </list>
    /// The final request body length may still be zero for the chunked or HTTP/2 scenarios.
    /// <para>
    /// This returns false when:
    /// <list type="bullet">
    /// <item><description>
    /// It's an HTTP/1.x request with no Content-Length or 'Transfer-Encoding: chunked' header, or the Content-Length is 0.
    /// </description></item>
    /// <item><description>
    /// It's an HTTP/1.x request with Connection: Upgrade (e.g. WebSockets). There is no HTTP request body for these requests and
    /// no data should be received until after the upgrade.
    /// </description></item>
    /// <item><description>
    /// It's an HTTP/2 request that set END_STREAM on the initial headers frame.
    /// </description></item>
    /// </list>
    /// </para>
    /// When false, the request body should never return data.
    /// </remarks>
    bool CanHaveBody { get; }
}
