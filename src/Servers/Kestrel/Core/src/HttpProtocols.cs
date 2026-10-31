// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// HTTP protocol versions
/// </summary>
[Flags]
public enum HttpProtocols
{
    /// <summary>
    /// No HTTP protocol version was specified.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// The HTTP/1.0 protocol version.
    /// </summary>
    Http1 = 0x1,

    /// <summary>
    /// The HTTP/2.0 protocol version.
    /// </summary>
    Http2 = 0x2,

    /// <summary>
    /// The <see cref="Http1"/> and <see cref="Http2"/> protocol versions.
    /// </summary>
    Http1AndHttp2 = Http1 | Http2,

    /// <summary>
    /// The HTTP/3.0 protocol version.
    /// </summary>
    Http3 = 0x4,

    /// <summary>
    /// The <see cref="Http1"/>, <see cref="Http2"/>, and <see cref="Http3"/> protocol versions.
    /// </summary>
    Http1AndHttp2AndHttp3 = Http1 | Http2 | Http3
}
