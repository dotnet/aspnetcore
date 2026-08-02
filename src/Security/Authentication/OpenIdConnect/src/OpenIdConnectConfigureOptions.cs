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
    private static readonly Func<string, TimeSpan?> _invariantNullableTimeSpanParse = (string timespanString) => TimeSpan.Parse(timespanString, CultureInfo.InvariantCulture);

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

        options.AccessDeniedPath = new PathString(configSection[nameof(options.AccessDeniedPath)] ?? options.AccessDeniedPath.Value);
        options.Authority = configSection[nameof(options.Authority)] ?? options.Authority;
        options.AutomaticRefreshInterval = StringHelpers.ParseValueOrDefault(configSection[nameof(options.AutomaticRefreshInterval)], _invariantTimeSpanParse, options.AutomaticRefreshInterval);
        options.BackchannelTimeout = StringHelpers.ParseValueOrDefault(configSection[nameof(options.BackchannelTimeout)], _invariantTimeSpanParse, options.BackchannelTimeout);
        options.CallbackPath = new PathString(configSection[nameof(options.CallbackPath)] ?? options.CallbackPath.Value);
        options.ClaimsIssuer = configSection[nameof(options.ClaimsIssuer)] ?? options.ClaimsIssuer;
        options.ClientId = configSection[nameof(options.ClientId)] ?? options.ClientId;
        options.ClientSecret = configSection[nameof(options.ClientSecret)] ?? options.ClientSecret;
        SetCookieFromConfig(configSection.GetSection(nameof(options.CorrelationCookie)), options.CorrelationCookie);
        options.DisableTelemetry = StringHelpers.ParseValueOrDefault(configSection[nameof(options.DisableTelemetry)], bool.Parse, options.DisableTelemetry);
        options.ForwardAuthenticate = configSection[nameof(options.ForwardAuthenticate)] ?? options.ForwardAuthenticate;
        options.ForwardChallenge = configSection[nameof(options.ForwardChallenge)] ?? options.ForwardChallenge;
        options.ForwardDefault = configSection[nameof(options.ForwardDefault)] ?? options.ForwardDefault;
        options.ForwardForbid = configSection[nameof(options.ForwardForbid)] ?? options.ForwardForbid;
        options.ForwardSignIn = configSection[nameof(options.ForwardSignIn)] ?? options.ForwardSignIn;
        options.ForwardSignOut = configSection[nameof(options.ForwardSignOut)] ?? options.ForwardSignOut;
        options.GetClaimsFromUserInfoEndpoint = StringHelpers.ParseValueOrDefault(configSection[nameof(options.GetClaimsFromUserInfoEndpoint)], bool.Parse, options.GetClaimsFromUserInfoEndpoint);
        options.MapInboundClaims = StringHelpers.ParseValueOrDefault(configSection[nameof(options.MapInboundClaims)], bool.Parse, options.MapInboundClaims);
        options.MaxAge = StringHelpers.ParseValueOrDefault(configSection[nameof(options.MaxAge)], _invariantNullableTimeSpanParse, options.MaxAge);
        options.MetadataAddress = configSection[nameof(options.MetadataAddress)] ?? options.MetadataAddress;
        SetCookieFromConfig(configSection.GetSection(nameof(options.NonceCookie)), options.NonceCookie);
        options.Prompt = configSection[nameof(options.Prompt)] ?? options.Prompt;
        options.RefreshInterval = StringHelpers.ParseValueOrDefault(configSection[nameof(options.RefreshInterval)], _invariantTimeSpanParse, options.RefreshInterval);
        options.RefreshOnIssuerKeyNotFound = StringHelpers.ParseValueOrDefault(configSection[nameof(options.RefreshOnIssuerKeyNotFound)], bool.Parse, options.RefreshOnIssuerKeyNotFound);
        options.RemoteAuthenticationTimeout = StringHelpers.ParseValueOrDefault(configSection[nameof(options.RemoteAuthenticationTimeout)], _invariantTimeSpanParse, options.RemoteAuthenticationTimeout);
        options.RemoteSignOutPath = new PathString(configSection[nameof(options.RemoteSignOutPath)] ?? options.RemoteSignOutPath.Value);
        options.RequireHttpsMetadata = StringHelpers.ParseValueOrDefault(configSection[nameof(options.RequireHttpsMetadata)], bool.Parse, options.RequireHttpsMetadata);
        options.Resource = configSection[nameof(options.Resource)] ?? options.Resource;
        options.ResponseMode = configSection[nameof(options.ResponseMode)] ?? options.ResponseMode;
        options.ResponseType = configSection[nameof(options.ResponseType)] ?? options.ResponseType;
        options.ReturnUrlParameter = configSection[nameof(options.ReturnUrlParameter)] ?? options.ReturnUrlParameter;
        options.SaveTokens = StringHelpers.ParseValueOrDefault(configSection[nameof(options.SaveTokens)], bool.Parse, options.SaveTokens);
        ClearAndSetListOption(options.Scope, configSection.GetSection(nameof(options.Scope)));
        options.SignedOutCallbackPath = new PathString(configSection[nameof(options.SignedOutCallbackPath)] ?? options.SignedOutCallbackPath.Value);
        options.SignedOutRedirectUri = configSection[nameof(options.SignedOutRedirectUri)] ?? options.SignedOutRedirectUri;
        options.SignInScheme = configSection[nameof(options.SignInScheme)] ?? options.SignInScheme;
        options.SignOutScheme = configSection[nameof(options.SignOutScheme)] ?? options.SignOutScheme;
        options.SkipUnrecognizedRequests = StringHelpers.ParseValueOrDefault(configSection[nameof(options.SkipUnrecognizedRequests)], bool.Parse, options.SkipUnrecognizedRequests);
        options.UsePkce = StringHelpers.ParseValueOrDefault(configSection[nameof(options.UsePkce)], bool.Parse, options.UsePkce);
        options.UseTokenLifetime = StringHelpers.ParseValueOrDefault(configSection[nameof(options.UseTokenLifetime)], bool.Parse, options.UseTokenLifetime);
    }

    private static void SetCookieFromConfig(IConfiguration cookieConfigSection, CookieBuilder cookieBuilder)
    {
        if (cookieConfigSection is null || !cookieConfigSection.GetChildren().Any())
        {
            return;
        }

        // Override the existing defaults when values are set instead of constructing
        // an entirely new CookieBuilder.
        cookieBuilder.Domain = cookieConfigSection[nameof(cookieBuilder.Domain)] ?? cookieBuilder.Domain;
        cookieBuilder.HttpOnly = StringHelpers.ParseValueOrDefault(cookieConfigSection[nameof(cookieBuilder.HttpOnly)], bool.Parse, cookieBuilder.HttpOnly);
        cookieBuilder.IsEssential = StringHelpers.ParseValueOrDefault(cookieConfigSection[nameof(cookieBuilder.IsEssential)], bool.Parse, cookieBuilder.IsEssential);
        cookieBuilder.Expiration = StringHelpers.ParseValueOrDefault(cookieConfigSection[nameof(cookieBuilder.Expiration)], _invariantNullableTimeSpanParse, cookieBuilder.Expiration);
        cookieBuilder.MaxAge = StringHelpers.ParseValueOrDefault<TimeSpan?>(cookieConfigSection[nameof(cookieBuilder.MaxAge)], _invariantNullableTimeSpanParse, cookieBuilder.MaxAge);
        cookieBuilder.Name = cookieConfigSection[nameof(CookieBuilder.Name)] ?? cookieBuilder.Name;
        cookieBuilder.Path = cookieConfigSection[nameof(CookieBuilder.Path)] ?? cookieBuilder.Path;
        cookieBuilder.SameSite = cookieConfigSection[nameof(CookieBuilder.SameSite)] is string sameSiteMode
            ? Enum.Parse<SameSiteMode>(sameSiteMode, ignoreCase: true)
            : cookieBuilder.SameSite;
        cookieBuilder.SecurePolicy = cookieConfigSection[nameof(CookieBuilder.SecurePolicy)] is string securePolicy
            ? Enum.Parse<CookieSecurePolicy>(securePolicy, ignoreCase: true)
            : cookieBuilder.SecurePolicy;
        ClearAndSetListOption(cookieBuilder.Extensions, cookieConfigSection.GetSection(nameof(cookieBuilder.Extensions)));
    }

    private static void ClearAndSetListOption(ICollection<string> listOption, IConfigurationSection listConfigSection)
    {
        var elementsFromConfig = listConfigSection.GetChildren().Select(element => element.Value).OfType<string>();
        if (elementsFromConfig.Any())
        {
            listOption.Clear();
            foreach (var element in elementsFromConfig)
            {
                listOption.Add(element);
            }
        }
    }

    /// <inheritdoc />
    public void Configure(OpenIdConnectOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}
