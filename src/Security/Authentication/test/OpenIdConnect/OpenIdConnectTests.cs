// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

public class OpenIdConnectTests
{
    static readonly string noncePrefix = "OpenIdConnect." + "Nonce.";
    static readonly string nonceDelimiter = ".";
    const string DefaultHost = @"https://example.com";

    /// <summary>
    /// Tests RedirectForSignOutContext replaces the OpenIdConnectMesssage correctly.
    /// </summary>
    /// <returns>Task</returns>
    [Fact]
    public async Task SignOutSettingMessage()
    {
        var setting = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opt.Configuration = new OpenIdConnectConfiguration
            {
                EndSessionEndpoint = "https://example.com/signout_test/signout_request"
            };
        });

        var server = setting.CreateTestServer();

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Signout);
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        setting.ValidateSignoutRedirect(
            transaction.Response.Headers.Location,
            OpenIdConnectParameterNames.SkuTelemetry,
            OpenIdConnectParameterNames.VersionTelemetry);
    }

    [Fact]
    public async Task RedirectToIdentityProvider_SetsNonceCookiePath_ToCallBackPath()
    {
        var setting = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opt.Configuration = new OpenIdConnectConfiguration
            {
                AuthorizationEndpoint = "https://example.com/provider/login"
            };
        });

        var server = setting.CreateTestServer();

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Challenge);
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
        var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
        var nonce = Assert.Single(setCookie.Value, v => v.StartsWith(OpenIdConnectDefaults.CookieNoncePrefix, StringComparison.Ordinal));
        Assert.Contains("path=/signin-oidc", nonce);
    }

    [Fact]
    public async Task RedirectToIdentityProvider_NonceCookieOptions_CanBeOverriden()
    {
        var setting = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opt.Configuration = new OpenIdConnectConfiguration
            {
                AuthorizationEndpoint = "https://example.com/provider/login"
            };
            opt.NonceCookie.Path = "/";
            opt.NonceCookie.Extensions.Add("ExtN");
        });

        var server = setting.CreateTestServer();

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Challenge);
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
        var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
        var nonce = Assert.Single(setCookie.Value, v => v.StartsWith(OpenIdConnectDefaults.CookieNoncePrefix, StringComparison.Ordinal));
        Assert.Contains("path=/", nonce);
        Assert.Contains("ExtN", nonce);
    }

    [Fact]
    public async Task RedirectToIdentityProvider_SetsCorrelationIdCookiePath_ToCallBackPath()
    {
        var setting = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opt.Configuration = new OpenIdConnectConfiguration
            {
                AuthorizationEndpoint = "https://example.com/provider/login"
            };
        });

        var server = setting.CreateTestServer();

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Challenge);
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
        var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
        var correlation = Assert.Single(setCookie.Value, v => v.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
        Assert.Contains("path=/signin-oidc", correlation);
    }

    [Fact]
    public async Task RedirectToIdentityProvider_CorrelationIdCookieOptions_CanBeOverriden()
    {
        var setting = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opt.Configuration = new OpenIdConnectConfiguration
            {
                AuthorizationEndpoint = "https://example.com/provider/login"
            };
            opt.CorrelationCookie.Path = "/";
            opt.CorrelationCookie.Extensions.Add("ExtC");
        });

        var server = setting.CreateTestServer();

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Challenge);
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
        var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
        var correlation = Assert.Single(setCookie.Value, v => v.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
        Assert.Contains("path=/", correlation);
        Assert.EndsWith("ExtC", correlation);
    }

    [Fact]
    public async Task EndSessionRequestDoesNotIncludeTelemetryParametersWhenDisabled()
    {
        var configuration = TestServerBuilder.CreateDefaultOpenIdConnectConfiguration();
        var setting = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Configuration = configuration;
            opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opt.DisableTelemetry = true;
        });

        var server = setting.CreateTestServer();

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Signout);
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.DoesNotContain(OpenIdConnectParameterNames.SkuTelemetry, res.Headers.Location.Query);
        Assert.DoesNotContain(OpenIdConnectParameterNames.VersionTelemetry, res.Headers.Location.Query);
        setting.ValidateSignoutRedirect(transaction.Response.Headers.Location);
    }

    [Fact]
    public async Task SignOutFormPostWithDefaultRedirectUri()
    {
        var settings = new TestSettings(o =>
        {
            o.AuthenticationMethod = OpenIdConnectRedirectBehavior.FormPost;
            o.Authority = TestServerBuilder.DefaultAuthority;
            o.ClientId = "Test Id";
        });
        var server = settings.CreateTestServer();

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Signout);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);

        settings.ValidateSignoutFormPost(transaction,
            OpenIdConnectParameterNames.PostLogoutRedirectUri);
    }

    [Fact]
    public async Task SignOutRedirectWithDefaultRedirectUri()
    {
        var settings = new TestSettings(o =>
        {
            o.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
            o.Authority = TestServerBuilder.DefaultAuthority;
            o.ClientId = "Test Id";
        });
        var server = settings.CreateTestServer();

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Signout);
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        settings.ValidateSignoutRedirect(transaction.Response.Headers.Location,
            OpenIdConnectParameterNames.PostLogoutRedirectUri);
    }

    [Fact]
    public async Task SignOutWithCustomRedirectUri()
    {
        var configuration = TestServerBuilder.CreateDefaultOpenIdConnectConfiguration();
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("OIDCTest"));
        var server = TestServerBuilder.CreateServer(o =>
        {
            o.Authority = TestServerBuilder.DefaultAuthority;
            o.ClientId = "Test Id";
            o.Configuration = configuration;
            o.StateDataFormat = stateFormat;
            o.SignedOutCallbackPath = "/thelogout";
            o.SignedOutRedirectUri = "https://example.com/postlogout";
        });

        var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Signout);
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        var query = transaction.Response.Headers.Location.Query.Substring(1).Split('&')
                               .Select(each => each.Split('='))
                               .ToDictionary(pair => pair[0], pair => pair[1]);

        string redirectUri;
        Assert.True(query.TryGetValue("post_logout_redirect_uri", out redirectUri));
        Assert.Equal(UrlEncoder.Default.Encode("https://example.com/thelogout"), redirectUri, true);

        string state;
        Assert.True(query.TryGetValue("state", out state));
        var properties = stateFormat.Unprotect(state);
        Assert.Equal("https://example.com/postlogout", properties.RedirectUri, true);
    }

    [Fact]
    public async Task SignOutWith_Specific_RedirectUri_From_Authentication_Properites()
    {
        var configuration = TestServerBuilder.CreateDefaultOpenIdConnectConfiguration();
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("OIDCTest"));
        var server = TestServerBuilder.CreateServer(o =>
        {
            o.Authority = TestServerBuilder.DefaultAuthority;
            o.StateDataFormat = stateFormat;
            o.ClientId = "Test Id";
            o.Configuration = configuration;
            o.SignedOutRedirectUri = "https://example.com/postlogout";
        });

        var transaction = await server.SendAsync("https://example.com/signout_with_specific_redirect_uri");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        var query = transaction.Response.Headers.Location.Query.Substring(1).Split('&')
                               .Select(each => each.Split('='))
                               .ToDictionary(pair => pair[0], pair => pair[1]);

        string redirectUri;
        Assert.True(query.TryGetValue("post_logout_redirect_uri", out redirectUri));
        Assert.Equal(UrlEncoder.Default.Encode("https://example.com/signout-callback-oidc"), redirectUri, true);

        string state;
        Assert.True(query.TryGetValue("state", out state));
        var properties = stateFormat.Unprotect(state);
        Assert.Equal("http://www.example.com/specific_redirect_uri", properties.RedirectUri, true);
    }

    [Fact]
    public async Task SignOut_WithMissingConfig_Throws()
    {
        var setting = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Configuration = new OpenIdConnectConfiguration();
        });
        var server = setting.CreateTestServer();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync(DefaultHost + TestServerBuilder.Signout));
        Assert.Equal("Cannot redirect to the end session endpoint, the configuration may be missing or invalid.", exception.Message);
    }

    [Fact]
    public async Task RemoteSignOut_WithMissingIssuer()
    {
        var settings = new TestSettings(o =>
        {
            o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.Authority = TestServerBuilder.DefaultAuthority;
            o.ClientId = "Test Id";
        });
        var server = settings.CreateTestServer(handler: async context =>
        {
            var claimsIdentity = new ClaimsIdentity("Cookies");
            claimsIdentity.AddClaim(new Claim("iss", "test"));
            await context.SignInAsync(new ClaimsPrincipal(claimsIdentity));
        });

        var signInTransaction = await server.SendAsync(DefaultHost);

        var remoteSignOutTransaction = await server.SendAsync(DefaultHost + "/signout-oidc", signInTransaction.AuthenticationCookieValue);
        Assert.Equal(HttpStatusCode.OK, remoteSignOutTransaction.Response.StatusCode);
        Assert.DoesNotContain(remoteSignOutTransaction.Response.Headers, h => h.Key == "Set-Cookie");

    }

    [Fact]
    public async Task RemoteSignOut_WithInvalidIssuer()
    {
        var settings = new TestSettings(o =>
        {
            o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.Authority = TestServerBuilder.DefaultAuthority;
            o.ClientId = "Test Id";
        });
        var server = settings.CreateTestServer(handler: async context =>
        {
            var claimsIdentity = new ClaimsIdentity("Cookies");
            claimsIdentity.AddClaim(new Claim("iss", "test"));
            await context.SignInAsync(new ClaimsPrincipal(claimsIdentity));
        });

        var signInTransaction = await server.SendAsync(DefaultHost);

        var remoteSignOutTransaction = await server.SendAsync(DefaultHost + "/signout-oidc?iss=invalid", signInTransaction.AuthenticationCookieValue);
        Assert.Equal(HttpStatusCode.OK, remoteSignOutTransaction.Response.StatusCode);
        Assert.DoesNotContain(remoteSignOutTransaction.Response.Headers, h => h.Key == "Set-Cookie");
    }

    [Fact]
    public async Task RemoteSignOut_Get_Successful()
    {
        var settings = new TestSettings(o =>
        {
            o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.Authority = TestServerBuilder.DefaultAuthority;
            o.ClientId = "Test Id";
        });
        var server = settings.CreateTestServer(handler: async context =>
        {
            var claimsIdentity = new ClaimsIdentity("Cookies");
            claimsIdentity.AddClaim(new Claim("iss", "test"));
            claimsIdentity.AddClaim(new Claim("sid", "something"));
            await context.SignInAsync(new ClaimsPrincipal(claimsIdentity));
        });

        var signInTransaction = await server.SendAsync(DefaultHost);

        var remoteSignOutTransaction = await server.SendAsync(DefaultHost + "/signout-oidc?iss=test&sid=something", signInTransaction.AuthenticationCookieValue);
        Assert.Equal(HttpStatusCode.OK, remoteSignOutTransaction.Response.StatusCode);
        Assert.Contains(remoteSignOutTransaction.Response.Headers, h => h.Key == "Set-Cookie");
    }

    [Fact]
    public void MapInboundClaimsDefaultsToTrue()
    {
        var options = new OpenIdConnectOptions();
        Assert.True(options.MapInboundClaims);
#pragma warning disable CS0618 // Type or member is obsolete
        var jwtHandler = options.SecurityTokenValidator as JwtSecurityTokenHandler;
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.NotNull(jwtHandler);
        Assert.True(jwtHandler.MapInboundClaims);
    }

    [Fact]
    public void MapInboundClaimsCanBeSetToFalse()
    {
        var options = new OpenIdConnectOptions();
        options.MapInboundClaims = false;
        Assert.False(options.MapInboundClaims);
#pragma warning disable CS0618 // Type or member is obsolete
        var jwtHandler = options.SecurityTokenValidator as JwtSecurityTokenHandler;
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.NotNull(jwtHandler);
        Assert.False(jwtHandler.MapInboundClaims);
    }

    // Test Cases for calculating the expiration time of cookie from cookie name
    [Fact]
    public void NonceCookieExpirationTime()
    {
        DateTime utcNow = DateTime.UtcNow;

        Assert.Equal(DateTime.MaxValue, GetNonceExpirationTime(noncePrefix + DateTime.MaxValue.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));

        Assert.Equal(DateTime.MinValue + TimeSpan.FromHours(1), GetNonceExpirationTime(noncePrefix + DateTime.MinValue.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));

        Assert.Equal(utcNow + TimeSpan.FromHours(1), GetNonceExpirationTime(noncePrefix + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));

        Assert.Equal(DateTime.MinValue, GetNonceExpirationTime(noncePrefix, TimeSpan.FromHours(1)));

        Assert.Equal(DateTime.MinValue, GetNonceExpirationTime("", TimeSpan.FromHours(1)));

        Assert.Equal(DateTime.MinValue, GetNonceExpirationTime(noncePrefix + noncePrefix, TimeSpan.FromHours(1)));

        Assert.Equal(utcNow + TimeSpan.FromHours(1), GetNonceExpirationTime(noncePrefix + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));

        Assert.Equal(DateTime.MinValue, GetNonceExpirationTime(utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void CanReadOpenIdConnectOptionsFromConfig()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:Authority", "https://authority.com"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:BackchannelTimeout", "00:05:00"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientId", "client-id"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientSecret", "client-secret"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:RequireHttpsMetadata", "false"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:CorrelationCookie:Domain", "https://localhost:5000"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:CorrelationCookie:Name", "CookieName"),
        }).Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        var builder = services.AddAuthentication();
        builder.AddOpenIdConnect();
        var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
        Assert.Equal("https://authority.com", options.Authority);
        Assert.Equal(options.BackchannelTimeout, TimeSpan.FromMinutes(5));
        Assert.False(options.RequireHttpsMetadata);
        Assert.False(options.GetClaimsFromUserInfoEndpoint); // Assert default values are respected
        Assert.Equal(new PathString("/signin-oidc"), options.CallbackPath); // Assert default callback paths are respected
        Assert.Equal("https://localhost:5000", options.CorrelationCookie.Domain); // Can set nested properties on cookie
        Assert.Equal("CookieName", options.CorrelationCookie.Name);
    }

    [Fact]
    public void CanCreateOpenIdConnectCookiesFromConfig()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:Authority", "https://authority.com"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:BackchannelTimeout", ""),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientId", "client-id"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientSecret", "client-secret"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:CorrelationCookie:Domain", "https://localhost:5000"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:CorrelationCookie:IsEssential", "False"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:CorrelationCookie:SecurePolicy", "always"),
        }).Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        var builder = services.AddAuthentication();
        builder.AddOpenIdConnect();
        var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
        Assert.Equal("https://localhost:5000", options.CorrelationCookie.Domain);
        Assert.False(options.CorrelationCookie.IsEssential);
        Assert.Equal(CookieSecurePolicy.Always, options.CorrelationCookie.SecurePolicy);
        // Default values are respected
        Assert.Equal(".AspNetCore.Correlation.", options.CorrelationCookie.Name);
        Assert.True(options.CorrelationCookie.HttpOnly);
        Assert.Equal(SameSiteMode.None, options.CorrelationCookie.SameSite);
        Assert.Equal(OpenIdConnectDefaults.CookieNoncePrefix, options.NonceCookie.Name);
        Assert.True(options.NonceCookie.IsEssential);
        Assert.True(options.NonceCookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.NonceCookie.SecurePolicy);
        Assert.Equal(TimeSpan.FromMinutes(1), options.BackchannelTimeout);
    }

    [Fact]
    public void ThrowsExceptionsWhenParsingInvalidOptionsFromConfig()
    {
        var services = new ServiceCollection().AddLogging();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:Authority", "https://authority.com"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:BackchannelTimeout", "definitelynotatimespan"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientId", "client-id"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientSecret", "client-secret"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:CorrelationCookie:IsEssential", "definitelynotaboolean"),
        }).Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        var builder = services.AddAuthentication();
        builder.AddOpenIdConnect();
        var sp = services.BuildServiceProvider();

        Assert.Throws<FormatException>(() =>
            sp.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme));
    }

    [Fact]
    public void ScopeOptionsCanBeOverwrittenFromOptions()
    {
        var services = new ServiceCollection().AddLogging();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:Authority", "https://authority.com"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientId", "client-id"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientSecret", "client-secret"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:Scope:0", "given_name"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:Scope:1", "birthdate"),
        }).Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        var builder = services.AddAuthentication();
        builder.AddOpenIdConnect();
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
        Assert.Equal(2, options.Scope.Count);
        Assert.DoesNotContain("openid", options.Scope);
        Assert.DoesNotContain("profile", options.Scope);
        Assert.Contains("given_name", options.Scope);
        Assert.Contains("birthdate", options.Scope);
    }

    [Fact]
    public void OptionsFromConfigCanBeOverwritten()
    {
        var services = new ServiceCollection().AddLogging();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:Authority", "https://authority.com"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientId", "client-id"),
            new KeyValuePair<string, string>("Authentication:Schemes:OpenIdConnect:ClientSecret", "client-secret"),
        }).Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        var builder = services.AddAuthentication();
        builder.AddOpenIdConnect(o =>
        {
            o.ClientSecret = "overwritten-client-secret";
        });
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
        Assert.Equal("client-id", options.ClientId);
        Assert.Equal("overwritten-client-secret", options.ClientSecret);
    }

    private static DateTime GetNonceExpirationTime(string keyname, TimeSpan nonceLifetime)
    {
        DateTime nonceTime = DateTime.MinValue;
        string timestamp = null;
        int endOfTimestamp;
        if (keyname.StartsWith(noncePrefix, StringComparison.Ordinal))
        {
            timestamp = keyname.Substring(noncePrefix.Length);
            endOfTimestamp = timestamp.IndexOf('.');

            if (endOfTimestamp != -1)
            {
                timestamp = timestamp.Substring(0, endOfTimestamp);
                try
                {
                    nonceTime = DateTime.FromBinary(Convert.ToInt64(timestamp, CultureInfo.InvariantCulture));
                    if ((nonceTime >= DateTime.UtcNow) && ((DateTime.MaxValue - nonceTime) < nonceLifetime))
                    {
                        nonceTime = DateTime.MaxValue;
                    }
                    else
                    {
                        nonceTime += nonceLifetime;
                    }
                }
                catch
                {
                }
            }
        }

        return nonceTime;
    }

}
