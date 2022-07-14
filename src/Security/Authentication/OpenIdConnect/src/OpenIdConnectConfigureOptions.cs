// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

internal sealed class OpenIdConnectConfigureOptions : IConfigureNamedOptions<OpenIdConnectOptions>
{
    private readonly IAuthenticationConfigurationProvider _authenticationConfigurationProvider;
    private static readonly Func<string, TimeSpan> _invariantTimeSpanParse = (string timespanString) => TimeSpan.Parse(timespanString, CultureInfo.InvariantCulture);

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

        options.AccessDeniedPath = new PathString(configSection[nameof(options.AccessDeniedPath)]);
        options.Authority = configSection[nameof(options.Authority)];
        options.AutomaticRefreshInterval = StringHelpers.ParseValueOrDefault(options.AutomaticRefreshInterval, configSection[nameof(options.AutomaticRefreshInterval)], _invariantTimeSpanParse);
        options.BackchannelTimeout = StringHelpers.ParseValueOrDefault(options.BackchannelTimeout, configSection[nameof(options.BackchannelTimeout)], _invariantTimeSpanParse);
        options.CallbackPath = new PathString(configSection[nameof(options.CallbackPath)] ?? options.CallbackPath);
        options.ClaimsIssuer = configSection[nameof(options.ClaimsIssuer)];
        options.ClientId = configSection[nameof(options.ClientId)];
        options.ClientSecret = configSection[nameof(options.ClientSecret)];
        options.CorrelationCookie = GetCookieFromConfig(configSection.GetSection(nameof(options.CorrelationCookie)), options.CorrelationCookie);
        options.DisableTelemetry = StringHelpers.ParseValueOrDefault(options.DisableTelemetry, configSection[nameof(options.DisableTelemetry)], bool.Parse);
        options.ForwardAuthenticate = configSection[nameof(options.ForwardAuthenticate)];
        options.ForwardChallenge = configSection[nameof(options.ForwardChallenge)];
        options.ForwardDefault = configSection[nameof(options.ForwardDefault)];
        options.ForwardForbid = configSection[nameof(options.ForwardForbid)];
        options.ForwardSignIn = configSection[nameof(options.ForwardSignIn)];
        options.ForwardSignOut = configSection[nameof(options.ForwardSignOut)];
        options.GetClaimsFromUserInfoEndpoint = StringHelpers.ParseValueOrDefault(options.GetClaimsFromUserInfoEndpoint, configSection[nameof(options.GetClaimsFromUserInfoEndpoint)], bool.Parse);
        options.MapInboundClaims = StringHelpers.ParseValueOrDefault(options.MapInboundClaims, configSection[nameof(options.MapInboundClaims)], bool.Parse);
        options.MaxAge = options.MaxAge is TimeSpan maxAge ? StringHelpers.ParseValueOrDefault(maxAge, configSection[nameof(options.MaxAge)], _invariantTimeSpanParse) : options.MaxAge;
        options.MetadataAddress = configSection[nameof(options.MetadataAddress)];
        options.NonceCookie = GetCookieFromConfig(configSection.GetSection(nameof(options.NonceCookie)), options.NonceCookie);
        options.Prompt = configSection[nameof(options.Prompt)];
        options.RefreshInterval = StringHelpers.ParseValueOrDefault(options.RefreshInterval, configSection[nameof(options.RefreshInterval)], _invariantTimeSpanParse);
        options.RefreshOnIssuerKeyNotFound = StringHelpers.ParseValueOrDefault(options.RefreshOnIssuerKeyNotFound, configSection[nameof(options.RefreshOnIssuerKeyNotFound)], bool.Parse);
        options.RemoteAuthenticationTimeout = StringHelpers.ParseValueOrDefault(options.RemoteAuthenticationTimeout, configSection[nameof(options.RemoteAuthenticationTimeout)], _invariantTimeSpanParse);
        options.RemoteSignOutPath = new PathString(configSection[nameof(options.RemoteSignOutPath)] ?? options.RemoteSignOutPath);
        options.RequireHttpsMetadata = StringHelpers.ParseValueOrDefault(options.RequireHttpsMetadata, configSection[nameof(options.RequireHttpsMetadata)], bool.Parse);
        options.Resource = configSection[nameof(options.Resource)];
        options.ResponseMode = configSection[nameof(options.ResponseMode)] ?? options.ResponseMode;
        options.ResponseType = configSection[nameof(options.ResponseType)] ?? options.ResponseType;
        options.ReturnUrlParameter = configSection[nameof(options.ReturnUrlParameter)] ?? options.ReturnUrlParameter;
        options.SaveTokens = StringHelpers.ParseValueOrDefault(options.SaveTokens, configSection[nameof(options.SaveTokens)], bool.Parse);
        var scopes = configSection.GetSection(nameof(options.Scope)).GetChildren().Select(scope => scope.Value).OfType<string>();
        foreach (var scope in scopes)
        {
            options.Scope.Add(scope);
        }
        options.SignedOutCallbackPath = new PathString(configSection[nameof(options.SignedOutCallbackPath)] ?? options.SignedOutCallbackPath);
        options.SignedOutRedirectUri = configSection[nameof(options.SignedOutRedirectUri)] ?? options.SignedOutRedirectUri;
        options.SignInScheme = configSection[nameof(options.SignInScheme)];
        options.SignOutScheme = configSection[nameof(options.SignOutScheme)];
        options.SkipUnrecognizedRequests = StringHelpers.ParseValueOrDefault(options.SkipUnrecognizedRequests, configSection[nameof(options.SkipUnrecognizedRequests)], bool.Parse);
        options.UsePkce = StringHelpers.ParseValueOrDefault(options.UsePkce, configSection[nameof(options.UsePkce)], bool.Parse);
        options.UseTokenLifetime = StringHelpers.ParseValueOrDefault(options.UseTokenLifetime, configSection[nameof(options.UseTokenLifetime)], bool.Parse);
    }

    private static CookieBuilder GetCookieFromConfig(IConfiguration cookieConfigSection, CookieBuilder cookieBuilder)
    {
        if (cookieConfigSection is not null && cookieConfigSection.GetChildren().Any())
        {
            // Override the existing defaults when values are set instead of constructing
            // an entirely new CookieBuilder.
            cookieBuilder.Domain = cookieConfigSection[nameof(cookieBuilder.Domain)];
            cookieBuilder.HttpOnly = StringHelpers.ParseValueOrDefault(cookieBuilder.HttpOnly, cookieConfigSection[nameof(cookieBuilder.HttpOnly)], bool.Parse);
            cookieBuilder.IsEssential = StringHelpers.ParseValueOrDefault(cookieBuilder.IsEssential, cookieConfigSection[nameof(cookieBuilder.IsEssential)], bool.Parse);
            cookieBuilder.Expiration = cookieBuilder.Expiration is TimeSpan expiration ? StringHelpers.ParseValueOrDefault(expiration, cookieConfigSection[nameof(cookieBuilder.Expiration)], _invariantTimeSpanParse) : cookieBuilder.Expiration;
            cookieBuilder.MaxAge = cookieBuilder.MaxAge is TimeSpan maxAge ? StringHelpers.ParseValueOrDefault(maxAge, cookieConfigSection[nameof(cookieBuilder.MaxAge)], _invariantTimeSpanParse) : cookieBuilder.MaxAge;
            cookieBuilder.Name = cookieConfigSection[nameof(CookieBuilder.Name)] ?? cookieBuilder.Name;
            cookieBuilder.Path = cookieConfigSection[nameof(CookieBuilder.Path)];
            cookieBuilder.SameSite = cookieConfigSection[nameof(CookieBuilder.SameSite)]?.ToLowerInvariant() switch
            {
                "lax" => SameSiteMode.Lax,
                "strict" => SameSiteMode.Strict,
                "unspecified" => SameSiteMode.Unspecified,
                "none" => SameSiteMode.None,
                null => cookieBuilder.SameSite,
                "" => cookieBuilder.SameSite,
                _ => throw new InvalidOperationException($"{nameof(CookieBuilder.SameSite)} option must be a valid {nameof(SameSiteMode)}")
            };
            cookieBuilder.SecurePolicy = cookieConfigSection[nameof(CookieBuilder.SecurePolicy)]?.ToLowerInvariant() switch
            {
                "always" => CookieSecurePolicy.Always,
                "none" => CookieSecurePolicy.None,
                "sameasrequest" => CookieSecurePolicy.SameAsRequest,
                null => cookieBuilder.SecurePolicy,
                "" => cookieBuilder.SecurePolicy,
                _ => throw new InvalidOperationException($"{nameof(CookieBuilder.SameSite)} option must be a valid {nameof(SameSiteMode)}")
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
