// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.Components.Server;

/// <summary>
/// Options to configure interactive Server components.
/// </summary>
public class ServerComponentsEndpointOptions
{
    /// <summary>
    /// Gets or sets a value that indicates whether compression is enabled for the WebSocket connections.
    /// </summary>
    public bool EnableWebSocketCompression { get; set; } = true;

    /// <summary>
    /// Gets or sets the <c>frame-ancestors</c> <c>Content-Security-Policy</c> to set in the
    /// <see cref="HttpResponse"/> when <see cref="EnableWebSocketCompression" /> is enabled.
    /// </summary>
    /// <remarks>
    /// <para>Setting this value to <see langword="null" /> will prevent the policy from being
    /// automatically applied, which might make the app vulnerable. Care must be taken to apply
    /// a policy in this case whenever the first document is rendered.
    /// </para>
    /// <para>
    /// A content security policy provides defense against security threats that can occur if
    /// the app uses compression and can be embedded in other origins. When compression is enabled
    /// embedding the app inside an <c>iframe</c> from other origins is prohibited.
    /// </para>
    /// <para>
    /// For more details see the security recommendations for Interactive Server Components in
    /// the official documentation.
    /// </para>
    /// </remarks>
    public string? ContentSecurityFrameAncestorPolicy { get; set; } = "'self'";

    /// <summary>
    /// Gets or sets a callback to configure the underlying <see cref="HttpConnectionDispatcherOptions"/>.
    /// If set, <see cref="ContentSecurityFrameAncestorPolicy"/> will be applied independent of the value of
    /// <see cref="EnableWebSocketCompression"/>.
    /// </summary>
    public Action<HttpConnectionDispatcherOptions> ConfigureConnectionOptions { get; set; }
}
