// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring the well-known passkey endpoints document.
/// </summary>
[Experimental("ASP0039", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public static class PasskeyEndpointsServiceCollectionExtensions
{
    /// <summary>
    /// Configures the locations advertised by the well-known passkey endpoints document, which
    /// describes where a user can create and manage passkeys for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">A callback used to configure the advertised locations.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <remarks>
    /// <para>
    /// Credential managers fetch this document to discover whether a site supports passkeys and
    /// where a user can create or manage them, which lets them offer to upgrade a saved password to
    /// a passkey without the user having to visit the site and find the relevant page.
    /// </para>
    /// <para>
    /// This only configures the document. Call <c>MapWellKnownPasskeyEndpoints</c> on the
    /// application to serve it.
    /// </para>
    /// <para>
    /// See <see href="https://w3c.github.io/webappsec-passkey-endpoints/"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// The following example advertises the passkey management page of an application:
    /// <code>
    /// builder.Services.AddPasskeyEndpoints(options =>
    /// {
    ///     options.Enroll = "/Account/Manage/Passkeys";
    ///     options.Manage = "/Account/Manage/Passkeys";
    /// });
    ///
    /// var app = builder.Build();
    ///
    /// app.MapWellKnownPasskeyEndpoints();
    /// </code>
    /// A request to <c>https://contoso.com/.well-known/passkey-endpoints</c> then responds with:
    /// <code>
    /// {
    ///   "enroll": "https://contoso.com/Account/Manage/Passkeys",
    ///   "manage": "https://contoso.com/Account/Manage/Passkeys"
    /// }
    /// </code>
    /// </example>
    [Experimental("ASP0039", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
    public static IServiceCollection AddPasskeyEndpoints(this IServiceCollection services, Action<PasskeyEndpointsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        return services;
    }
}
