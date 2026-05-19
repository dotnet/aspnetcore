// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure Microsoft Account OAuth authentication.
/// </summary>
public static class MicrosoftAccountExtensions
{
    /// <summary>
    /// Adds Microsoft Account OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="MicrosoftAccountDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Microsoft Account authentication allows application users to sign in with their work, school, or personal Microsoft account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder)
        => builder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, _ => { });

    /// <summary>
    /// Adds Microsoft Account OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="MicrosoftAccountDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Microsoft Account authentication allows application users to sign in with their work, school, or personal Microsoft account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="MicrosoftAccountOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, Action<MicrosoftAccountOptions> configureOptions)
        => builder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds Microsoft Account OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="MicrosoftAccountDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Microsoft Account authentication allows application users to sign in with their work, school, or personal Microsoft account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="MicrosoftAccountOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, string authenticationScheme, Action<MicrosoftAccountOptions> configureOptions)
        => builder.AddMicrosoftAccount(authenticationScheme, MicrosoftAccountDefaults.DisplayName, configureOptions);

    /// <summary>
    /// Adds Microsoft Account OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="MicrosoftAccountDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Microsoft Account authentication allows application users to sign in with their work, school, or personal Microsoft account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">A display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="MicrosoftAccountOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<MicrosoftAccountOptions> configureOptions)
        => builder.AddOAuth<MicrosoftAccountOptions, MicrosoftAccountHandler>(authenticationScheme, displayName, configureOptions);
}
