// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

internal sealed class OpenIdConnectConfigureOptions : IConfigureNamedOptions<OpenIdConnectOptions>
{
    private readonly IAuthenticationConfigurationProvider _authenticationConfigurationProvider;

    /// <summary>
    /// Initializes a new <see cref="OpenIdConnectConfigureOptions"/> given the configuration
    /// provided by the <paramref name="configurationProvider"/>.
    /// </summary>
    /// <param name="configurationProvider">An <see cref="IAuthenticationConfigurationProvider"/> instance.</param>
    public OpenIdConnectConfigureOptions(IAuthenticationConfigurationProvider configurationProvider)
    {
        _authenticationConfigurationProvider = configurationProvider;
    }

    /// <inheritdoc />
    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var configSection = _authenticationConfigurationProvider.GetSchemeConfiguration(name);

        if (configSection is null || !configSection.GetChildren().Any())
        {
            return;
        }

        options.AccessDeniedPath = configSection[nameof(options.AccessDeniedPath)];
        options.Authority = configSection[nameof(options.Authority)];
        options.AutomaticRefreshInterval = TimeSpan.TryParse(configSection[nameof(options.AutomaticRefreshInterval)], out var automaticRefreshInterval)
            ? automaticRefreshInterval
            : options.AutomaticRefreshInterval;
        options.BackchannelTimeout = TimeSpan.TryParse(configSection[nameof(options.BackchannelTimeout)], out var backChannelTimeout)
            ? backChannelTimeout
            : options.BackchannelTimeout;
        options.CallbackPath = new PathString(configSection[nameof(options.CallbackPath)] ?? options.CallbackPath);
        options.ClaimsIssuer = configSection[nameof(options.ClaimsIssuer)];
        options.ClientId = configSection[nameof(options.ClientId)];
        options.ClientSecret = configSection[nameof(options.ClientSecret)];
        options.CorrelationCookie = GetCookieFromConfig(configSection.GetSection(nameof(options.CorrelationCookie)), options.CorrelationCookie);
        options.DisableTelemetry = bool.TryParse(configSection[nameof(options.DisableTelemetry)], out var disableTelemetry)
            ? disableTelemetry
            : options.DisableTelemetry;
        options.ForwardAuthenticate = configSection[nameof(options.ForwardAuthenticate)];
        options.ForwardChallenge = configSection[nameof(options.ForwardChallenge)];
        options.ForwardDefault = configSection[nameof(options.ForwardDefault)];
        options.ForwardForbid = configSection[nameof(options.ForwardForbid)];
        options.ForwardSignIn = configSection[nameof(options.ForwardSignIn)];
        options.ForwardSignOut = configSection[nameof(options.ForwardSignOut)];
        options.GetClaimsFromUserInfoEndpoint = bool.TryParse(configSection[nameof(options.GetClaimsFromUserInfoEndpoint)], out var getClaimsFromUserInfoEndpoint)
            ? getClaimsFromUserInfoEndpoint
            : options.GetClaimsFromUserInfoEndpoint;
        options.MapInboundClaims = bool.TryParse(configSection[nameof(options.MapInboundClaims)], out var mapInboundClaims)
            ? mapInboundClaims
            : options.MapInboundClaims;
        options.MaxAge = TimeSpan.TryParse(configSection[nameof(options.MaxAge)], out var maxAge)
            ? maxAge
            : options.MaxAge;
        options.MetadataAddress = configSection[nameof(options.MetadataAddress)];
        options.NonceCookie = GetCookieFromConfig(configSection.GetSection(nameof(options.NonceCookie)), options.NonceCookie);
        options.Prompt = configSection[nameof(options.Prompt)];
        options.RefreshInterval = TimeSpan.TryParse(configSection[nameof(options.RefreshInterval)], out var refreshInterval)
            ? refreshInterval
            : options.RefreshInterval;
        options.RefreshOnIssuerKeyNotFound = bool.TryParse(configSection[nameof(options.RefreshOnIssuerKeyNotFound)], out var refreshOnIssuerKeyNotFound)
            ? refreshOnIssuerKeyNotFound
            : options.RefreshOnIssuerKeyNotFound;
        options.RemoteAuthenticationTimeout = TimeSpan.TryParse(configSection[nameof(options.RemoteAuthenticationTimeout)], out var remoteAuthenticationTimeout)
            ? remoteAuthenticationTimeout
            : options.RemoteAuthenticationTimeout;
        options.RemoteSignOutPath = new PathString(configSection[nameof(options.RemoteSignOutPath)] ?? options.RemoteSignOutPath);
        options.RequireHttpsMetadata = bool.TryParse(configSection[nameof(options.RequireHttpsMetadata)], out var requireHttpsMetadata)
            ? requireHttpsMetadata
            : options.RequireHttpsMetadata;
        options.Resource = configSection[nameof(options.Resource)];
        options.ResponseMode = configSection[nameof(options.ResponseMode)] ?? options.ResponseMode;
        options.ResponseType = configSection[nameof(options.ResponseType)] ?? options.ResponseType;
        options.ReturnUrlParameter = configSection[nameof(options.ReturnUrlParameter)] ?? options.ReturnUrlParameter;
        options.SaveTokens = bool.TryParse(configSection[nameof(options.SaveTokens)], out var saveTokens)
            ? saveTokens
            : options.SaveTokens;
        var scopes = configSection.GetSection(nameof(options.Scope)).GetChildren().Select(scope => scope.Value).OfType<string>();
        foreach (var scope in scopes)
        {
            options.Scope.Add(scope);
        }
        options.SignedOutCallbackPath = new PathString(configSection[nameof(options.SignedOutCallbackPath)] ?? options.SignedOutCallbackPath);
        options.SignedOutRedirectUri = configSection[nameof(options.SignedOutRedirectUri)] ?? options.SignedOutRedirectUri;
        options.SignInScheme = configSection[nameof(options.SignInScheme)];
        options.SignOutScheme = configSection[nameof(options.SignOutScheme)];
        options.SkipUnrecognizedRequests = bool.TryParse(configSection[nameof(options.SkipUnrecognizedRequests)], out var skipUnreccognizedRequests)
            ? skipUnreccognizedRequests
            : options.SkipUnrecognizedRequests;
        options.UsePkce = bool.TryParse(configSection[nameof(options.UsePkce)], out var usePkce)
            ? usePkce
            : options.UsePkce;
        options.UseTokenLifetime = bool.TryParse(configSection[nameof(options.UseTokenLifetime)], out var useTokenLifetime)
            ? useTokenLifetime
            : options.UseTokenLifetime;
    }

    private static CookieBuilder GetCookieFromConfig(IConfiguration cookieConfigSection, CookieBuilder cookieBuilder)
    {
        if (cookieConfigSection is not null && cookieConfigSection.GetChildren().Any())
        {
            // Defaults match those used in https://github.com/dotnet/aspnetcore/blob/69c7a87cd8f05da9c05c373edfc0337c9e864d9e/src/Security/Authentication/Core/src/RemoteAuthenticationOptions.cs#L24-L31
            // and https://github.com/dotnet/aspnetcore/blob/69c7a87cd8f05da9c05c373edfc0337c9e864d9e/src/Security/Authentication/OpenIdConnect/src/OpenIdConnectOptions.cs#L68-L76
            cookieBuilder = new()
            {
                Domain = cookieConfigSection[nameof(CookieBuilder.Domain)],
                HttpOnly = bool.TryParse(cookieConfigSection[nameof(CookieBuilder.HttpOnly)], out var httpOnly)
                    ? httpOnly
                    : cookieBuilder.HttpOnly,
                IsEssential = bool.TryParse(cookieConfigSection[nameof(CookieBuilder.IsEssential)], out var isEssential)
                    ? isEssential
                    : cookieBuilder.IsEssential,
                Expiration = TimeSpan.TryParse(cookieConfigSection[nameof(CookieBuilder.Expiration)], out var expiration)
                    ? expiration
                    : null,
                MaxAge = TimeSpan.TryParse(cookieConfigSection[nameof(CookieBuilder.MaxAge)], out var maxAge)
                    ? maxAge
                    : null,
                Name = cookieConfigSection[nameof(CookieBuilder.Name)] ?? cookieBuilder.Name,
                Path = cookieConfigSection[nameof(CookieBuilder.Path)],
                SameSite = cookieConfigSection[nameof(CookieBuilder.SameSite)] switch
                {
                    "Lax" => SameSiteMode.Lax,
                    "Strict" => SameSiteMode.Strict,
                    "Unspecified" => SameSiteMode.Unspecified,
                    "None" => SameSiteMode.None,
                    _ => cookieBuilder.SameSite
                },

                SecurePolicy = cookieConfigSection[nameof(CookieBuilder.SecurePolicy)] switch
                {
                    "Always" => CookieSecurePolicy.Always,
                    "None" => CookieSecurePolicy.None,
                    "SameAsRequest" => CookieSecurePolicy.SameAsRequest,
                    _ => cookieBuilder.SecurePolicy
                }
            };

            var extensions = cookieConfigSection.GetSection(nameof(CookieBuilder.Extensions)).GetChildren().Select(ext => ext.Value).OfType<string>();
            foreach (var extension in extensions)
            {
                cookieBuilder.Extensions.Add(extension);
            }
        }

        return cookieBuilder;
    }

    /// <inheritdoc />
    public void Configure(OpenIdConnectOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}
