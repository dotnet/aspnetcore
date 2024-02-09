// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server;

/// <summary>
/// Options to configure interactive Server components.
/// </summary>
public class ServerComponentsEndpointOptions
{
    /// <summary>
    /// Gets or sets the <c>frame-ancestors</c> <c>Content-Security-Policy</c> to set in the
    /// <see cref="HttpResponse"/> when <see cref="ConfigureWebSocketAcceptContext" /> is set.
    /// </summary>
    /// <remarks>
    /// <para>Setting this value to <see langword="null" /> will prevent the policy from being
    /// automatically applied, which might make the app vulnerable. Care must be taken to apply
    /// a policy in this case whenever the first document is rendered.
    /// </para>
    /// <para>
    /// A content security policy provides defense against security threats that can occur if
    /// the app uses compression and can be embedded in other origins. When compression is enabled,
    /// embedding the app inside an <c>iframe</c> from other origins is forbidden.
    /// </para>
    /// <para>
    /// For more details see the security recommendations for Interactive Server Components in
    /// the official documentation.
    /// </para>
    /// </remarks>
    public string? ContentSecurityFrameAncestorsPolicy { get; set; } = "'self'";

    /// <summary>
    /// Gets or sets a value that determines if WebSocket compression should be disabled.
    /// </summary>
    /// <remarks>
    /// WebSocket compression is enabled by default, but it can be disabled by setting this value to <see langword="true" />.
    /// When a callback for <see cref="ConfigureWebSocketAcceptContext"/> is provided, the value of this property will be
    /// ignored, whether compression is enabled or not will be determined by the callback, and the Content Security Policy
    /// will be applied according to the value of <see cref="ContentSecurityFrameAncestorsPolicy"/>.
    /// When compression is disabled and no callback is provided, the Content Security Policy header will not be set on the
    /// responses.
    /// </remarks>
    public bool DisableWebSocketCompression { get; set; }

    /// <summary>
    /// Gets or sets a function to configure the <see cref="WebSocketAcceptContext"/> for the websocket connections
    /// used by the server components.
    /// </summary>
    /// <remarks>
    /// By default, a policy that enables compression and sets a Content Security Policy for the frame ancestors
    /// defined in <see cref="ContentSecurityFrameAncestorsPolicy"/> will be applied.
    /// </remarks>
    public Func<HttpContext, WebSocketAcceptContext, Task>? ConfigureWebSocketAcceptContext { get; set; }
}
