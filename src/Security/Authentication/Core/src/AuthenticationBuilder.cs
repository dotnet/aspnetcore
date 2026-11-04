// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Used to configure authentication
/// </summary>
public class AuthenticationBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="AuthenticationBuilder"/>.
    /// </summary>
    /// <param name="services">The services being configured.</param>
    public AuthenticationBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// The services being configured.
    /// </summary>
    public virtual IServiceCollection Services { get; }

    private AuthenticationBuilder AddSchemeHelper<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(string authenticationScheme, string? displayName, Action<TOptions>? configureOptions)
        where TOptions : AuthenticationSchemeOptions, new()
        where THandler : class, IAuthenticationHandler
    {
        Services.Configure<AuthenticationOptions>(o =>
        {
            o.AddScheme(authenticationScheme, scheme =>
            {
                scheme.HandlerType = typeof(THandler);
                scheme.DisplayName = displayName;
            });
        });
        if (configureOptions != null)
        {
            Services.Configure(authenticationScheme, configureOptions);
        }
        Services.AddOptions<TOptions>(authenticationScheme).Validate(o =>
        {
            o.Validate(authenticationScheme);
            return true;
        });
        Services.AddTransient<THandler>();
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, PostConfigureAuthenticationSchemeOptions<TOptions>>());
        return this;
    }

    /// <summary>
    /// Adds a <see cref="AuthenticationScheme"/> which can be used by <see cref="IAuthenticationService"/>.
    /// </summary>
    /// <typeparam name="TOptions">The <see cref="AuthenticationSchemeOptions"/> type to configure the handler.</typeparam>
    /// <typeparam name="THandler">The <see cref="AuthenticationHandler{TOptions}"/> used to handle this scheme.</typeparam>
    /// <param name="authenticationScheme">The name of this scheme.</param>
    /// <param name="displayName">The display name of this scheme.</param>
    /// <param name="configureOptions">Used to configure the scheme options.</param>
    /// <returns>The builder.</returns>
    public virtual AuthenticationBuilder AddScheme<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(string authenticationScheme, string? displayName, Action<TOptions>? configureOptions)
        where TOptions : AuthenticationSchemeOptions, new()
        where THandler : AuthenticationHandler<TOptions>
        => AddSchemeHelper<TOptions, THandler>(authenticationScheme, displayName, configureOptions);

    /// <summary>
    /// Adds a <see cref="AuthenticationScheme"/> which can be used by <see cref="IAuthenticationService"/>.
    /// </summary>
    /// <typeparam name="TOptions">The <see cref="AuthenticationSchemeOptions"/> type to configure the handler.</typeparam>
    /// <typeparam name="THandler">The <see cref="AuthenticationHandler{TOptions}"/> used to handle this scheme.</typeparam>
    /// <param name="authenticationScheme">The name of this scheme.</param>
    /// <param name="configureOptions">Used to configure the scheme options.</param>
    /// <returns>The builder.</returns>
    public virtual AuthenticationBuilder AddScheme<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(string authenticationScheme, Action<TOptions>? configureOptions)
        where TOptions : AuthenticationSchemeOptions, new()
        where THandler : AuthenticationHandler<TOptions>
        => AddScheme<TOptions, THandler>(authenticationScheme, displayName: null, configureOptions: configureOptions);

    /// <summary>
    /// Adds a <see cref="RemoteAuthenticationHandler{TOptions}"/> based <see cref="AuthenticationScheme"/> that supports remote authentication
    /// which can be used by <see cref="IAuthenticationService"/>.
    /// </summary>
    /// <typeparam name="TOptions">The <see cref="RemoteAuthenticationOptions"/> type to configure the handler.</typeparam>
    /// <typeparam name="THandler">The <see cref="RemoteAuthenticationHandler{TOptions}"/> used to handle this scheme.</typeparam>
    /// <param name="authenticationScheme">The name of this scheme.</param>
    /// <param name="displayName">The display name of this scheme.</param>
    /// <param name="configureOptions">Used to configure the scheme options.</param>
    /// <returns>The builder.</returns>
    public virtual AuthenticationBuilder AddRemoteScheme<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(string authenticationScheme, string? displayName, Action<TOptions>? configureOptions)
        where TOptions : RemoteAuthenticationOptions, new()
        where THandler : RemoteAuthenticationHandler<TOptions>
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, EnsureSignInScheme<TOptions>>());
        return AddScheme<TOptions, THandler>(authenticationScheme, displayName, configureOptions: configureOptions);
    }

    /// <summary>
    /// Adds a <see cref="PolicySchemeHandler"/> based authentication handler which can be used to
    /// redirect to other authentication schemes.
    /// </summary>
    /// <param name="authenticationScheme">The name of this scheme.</param>
    /// <param name="displayName">The display name of this scheme.</param>
    /// <param name="configureOptions">Used to configure the scheme options.</param>
    /// <returns>The builder.</returns>
    public virtual AuthenticationBuilder AddPolicyScheme(string authenticationScheme, string? displayName, Action<PolicySchemeOptions> configureOptions)
        => AddSchemeHelper<PolicySchemeOptions, PolicySchemeHandler>(authenticationScheme, displayName, configureOptions);

    // Used to ensure that there's always a default sign in scheme that's not itself
    private sealed class EnsureSignInScheme<TOptions> : IPostConfigureOptions<TOptions> where TOptions : RemoteAuthenticationOptions
    {
        private readonly AuthenticationOptions _authOptions;

        public EnsureSignInScheme(IOptions<AuthenticationOptions> authOptions)
        {
            _authOptions = authOptions.Value;
        }

        public void PostConfigure(string? name, TOptions options)
        {
            options.SignInScheme ??= _authOptions.DefaultSignInScheme ?? _authOptions.DefaultScheme;
        }
    }

    // Set TimeProvider from DI on all options instances, if not already set by tests.
    private sealed class PostConfigureAuthenticationSchemeOptions<TOptions> : IPostConfigureOptions<TOptions>
        where TOptions : AuthenticationSchemeOptions
    {
        public PostConfigureAuthenticationSchemeOptions(TimeProvider timeProvider)
        {
            TimeProvider = timeProvider;
        }

        private TimeProvider TimeProvider { get; }

        public void PostConfigure(string? name, TOptions options)
        {
            options.TimeProvider ??= TimeProvider;
        }
    }
}
