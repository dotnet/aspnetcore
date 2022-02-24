// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// When a user configures the <see cref="OpenIdConnectHandler"/> to be notified prior to redirecting to an IdentityProvider
/// an instance of <see cref="RedirectContext"/> is passed to the <see cref="OpenIdConnectEvents.RedirectToIdentityProvider(RedirectContext)"/>
/// and <see cref="OpenIdConnectEvents.RedirectToIdentityProviderForSignOut(RedirectContext)"/>.
/// </summary>
public class RedirectContext : PropertiesContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="RedirectContext"/>.
    /// </summary>
    /// <inheritdoc />
    public RedirectContext(
        HttpContext context,
        AuthenticationScheme scheme,
        OpenIdConnectOptions options,
        AuthenticationProperties properties)
        : base(context, scheme, options, properties) { }

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
    /// </summary>
    public OpenIdConnectMessage ProtocolMessage { get; set; } = default!;

    /// <summary>
    /// If true, will skip any default logic for this redirect.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Skips any default logic for this redirect.
    /// </summary>
    public void HandleResponse() => Handled = true;
}
