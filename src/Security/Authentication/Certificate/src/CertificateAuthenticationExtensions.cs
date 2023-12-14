// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication.Certificate;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to add Certificate authentication capabilities to an HTTP application pipeline.
/// </summary>
public static class CertificateAuthenticationAppBuilderExtensions
{
    /// <summary>
    /// Adds certificate authentication.
    /// <para>
    /// Certificate authentication uses a authentication handler that validates client certificate and
    /// raises an event where the certificate is resolved to a <see cref="ClaimsPrincipal"/>.
    /// See <see href="https://tools.ietf.org/html/rfc5246#section-7.4.4"/> to read more about certificate authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
    public static AuthenticationBuilder AddCertificate(this AuthenticationBuilder builder)
        => builder.AddCertificate(CertificateAuthenticationDefaults.AuthenticationScheme);

    /// <summary>
    /// Adds certificate authentication.
    /// <para>
    /// Certificate authentication uses a authentication handler that validates client certificate and
    /// raises an event where the certificate is resolved to a <see cref="ClaimsPrincipal"/>.
    /// See <see href="https://tools.ietf.org/html/rfc5246#section-7.4.4"/> to read more about certificate authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
    public static AuthenticationBuilder AddCertificate(this AuthenticationBuilder builder, string authenticationScheme)
        => builder.AddCertificate(authenticationScheme, configureOptions: null);

    /// <summary>
    /// Adds certificate authentication.
    /// <para>
    /// Certificate authentication uses a authentication handler that validates client certificate and
    /// raises an event where the certificate is resolved to a <see cref="ClaimsPrincipal"/>.
    /// See <see href="https://tools.ietf.org/html/rfc5246#section-7.4.4"/> to read more about certificate authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="CertificateAuthenticationOptions"/>.</param>
    /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
    public static AuthenticationBuilder AddCertificate(this AuthenticationBuilder builder, Action<CertificateAuthenticationOptions>? configureOptions)
        => builder.AddCertificate(CertificateAuthenticationDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds certificate authentication.
    /// <para>
    /// Certificate authentication uses a authentication handler that validates client certificate and
    /// raises an event where the certificate is resolved to a <see cref="ClaimsPrincipal"/>.
    /// See <see href="https://tools.ietf.org/html/rfc5246#section-7.4.4"/> to read more about certificate authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="CertificateAuthenticationOptions"/>.</param>
    /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
    public static AuthenticationBuilder AddCertificate(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<CertificateAuthenticationOptions>? configureOptions)
        => builder.AddScheme<CertificateAuthenticationOptions, CertificateAuthenticationHandler>(authenticationScheme, configureOptions);

    /// <summary>
    /// Adds certificate authentication.
    /// <para>
    /// Certificate authentication uses a authentication handler that validates client certificate and
    /// raises an event where the certificate is resolved to a <see cref="ClaimsPrincipal"/>.
    /// See <see href="https://tools.ietf.org/html/rfc5246#section-7.4.4"/> to read more about certicate authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="CertificateValidationCacheOptions"/>.</param>
    /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
    public static AuthenticationBuilder AddCertificateCache(
        this AuthenticationBuilder builder,
        Action<CertificateValidationCacheOptions>? configureOptions = null)
    {
        builder.Services.AddSingleton<ICertificateValidationCache, CertificateValidationCache>();
        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }
        return builder;
    }
}
