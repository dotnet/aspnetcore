// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// Implemented by HTTP/1.x parsing handlers that want to be notified when a request uses a bare LF
/// line terminator instead of CRLF.
/// </summary>
internal interface IBareLineFeedTracker
{
    /// <summary>
    /// Called when an HTTP/1.x line terminator is a bare LF instead of CRLF.
    /// </summary>
    /// <param name="rejected"><see langword="true"/> when the bare LF caused the request to be rejected; otherwise <see langword="false"/>.</param>
    void OnBareLineFeedTerminator(bool rejected);
}
