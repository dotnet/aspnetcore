// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authentication.Negotiate.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for enabling Negotiate authentication.
/// </summary>
public static class NegotiateExtensions
{
    /// <summary>
    /// Configures the <see cref="AuthenticationBuilder"/> to use Negotiate (also known as Windows, Kerberos, or NTLM) authentication
    /// using the default scheme from <see cref="NegotiateDefaults.AuthenticationScheme"/>.
    /// <para>
    /// This authentication handler supports Kerberos on Windows and Linux servers.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>The original builder.</returns>
    public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder)
        => builder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, _ => { });

    /// <summary>
    /// Configures the <see cref="AuthenticationBuilder"/> to use Negotiate (also known as Windows, Kerberos, or NTLM) authentication
    /// using the default scheme. The default scheme is specified by <see cref="NegotiateDefaults.AuthenticationScheme"/>.
    /// <para>
    /// This authentication handler supports Kerberos on Windows and Linux servers.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
    /// <returns>The original builder.</returns>
    public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, Action<NegotiateOptions> configureOptions)
        => builder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Configures the <see cref="AuthenticationBuilder"/> to use Negotiate (also known as Windows, Kerberos, or NTLM) authentication
    /// using the specified authentication scheme.
    /// <para>
    /// This authentication handler supports Kerberos on Windows and Linux servers.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The scheme name used to identify the authentication handler internally.</param>
    /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
    /// <returns>The original builder.</returns>
    public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, string authenticationScheme, Action<NegotiateOptions> configureOptions)
        => builder.AddNegotiate(authenticationScheme, displayName: null, configureOptions: configureOptions);

    /// <summary>
    /// Configures the <see cref="AuthenticationBuilder"/> to use Negotiate (also known as Windows, Kerberos, or NTLM) authentication
    /// using the specified authentication scheme.
    /// <para>
    /// This authentication handler supports Kerberos on Windows and Linux servers.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The scheme name used to identify the authentication handler internally.</param>
    /// <param name="displayName">The name displayed to users when selecting an authentication handler. The default is null to prevent this from displaying.</param>
    /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
    /// <returns>The original builder.</returns>
    public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, string authenticationScheme, string? displayName, Action<NegotiateOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<NegotiateOptions>, PostConfigureNegotiateOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter>(new NegotiateOptionsValidationStartupFilter(authenticationScheme)));
        return builder.AddScheme<NegotiateOptions, NegotiateHandler>(authenticationScheme, displayName, configureOptions);
    }
}
