// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for adding the well-known passkey endpoints document to an application.
/// </summary>
public static class PasskeyEndpointsServiceCollectionExtensions
{
    /// <summary>
    /// Serves the well-known passkey endpoints document at <c>/.well-known/passkey-endpoints</c>,
    /// which advertises where a user can create and manage passkeys for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">A callback used to configure the advertised endpoints.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <remarks>
    /// <para>
    /// Credential managers fetch this document to discover whether a site supports passkeys and
    /// where a user can create or manage them, which lets them offer to upgrade a saved password to
    /// a passkey without the user having to visit the site and find the relevant page.
    /// </para>
    /// <para>
    /// The document is served by middleware that runs before routing, so it is always served at the
    /// root of the origin, as the specification requires. Requests are answered without
    /// authentication, because credential managers fetch the document without a user session and
    /// the specification does not allow a redirect to be returned.
    /// </para>
    /// <para>
    /// If neither <see cref="PasskeyEndpointsOptions.Enroll"/> nor
    /// <see cref="PasskeyEndpointsOptions.Manage"/> is configured, a warning is logged and the
    /// document is not served, because advertising no endpoints would claim support for passkeys
    /// without giving a credential manager anywhere to send the user.
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
    /// </code>
    /// A request to <c>https://contoso.com/.well-known/passkey-endpoints</c> then responds with:
    /// <code>
    /// {
    ///   "enroll": "https://contoso.com/Account/Manage/Passkeys",
    ///   "manage": "https://contoso.com/Account/Manage/Passkeys"
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddPasskeyEndpoints(this IServiceCollection services, Action<PasskeyEndpointsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, PasskeyEndpointsStartupFilter>());

        return services;
    }
}
