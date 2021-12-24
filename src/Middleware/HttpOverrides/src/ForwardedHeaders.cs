// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpOverrides;

/// <summary>
/// Flags for controlling which forwarders are processed.
/// </summary>
[Flags]
public enum ForwardedHeaders
{
    /// <summary>
    /// Do not process any forwarders
    /// </summary>
    None = 0,
    /// <summary>
    /// Process X-Forwarded-For, which identifies the originating IP address of the client.
    /// </summary>
    XForwardedFor = 1 << 0,
    /// <summary>
    /// Process X-Forwarded-Host, which identifies the original host requested by the client.
    /// </summary>
    XForwardedHost = 1 << 1,
    /// <summary>
    /// Process X-Forwarded-Proto, which identifies the protocol (HTTP or HTTPS) the client used to connect.
    /// </summary>
    XForwardedProto = 1 << 2,
    /// <summary>
    /// Process X-Forwarded-For, X-Forwarded-Host and X-Forwarded-Proto.
    /// </summary>
    All = XForwardedFor | XForwardedHost | XForwardedProto
}
