// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect
{
    public class OpenIdConnectTests
    {
        static string noncePrefix = "OpenIdConnect." + "Nonce.";
        static string nonceDelimiter = ".";
        const string DefaultHost = @"https://example.com";
        const string Logout = "/logout";
        const string Signin = "/signin";
        const string Signout = "/signout";

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
            var nonce = Assert.Single(setCookie.Value, v => v.StartsWith(OpenIdConnectDefaults.CookieNoncePrefix));
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
            });

            var server = setting.CreateTestServer();

            var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Challenge);
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);
            var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
            var nonce = Assert.Single(setCookie.Value, v => v.StartsWith(OpenIdConnectDefaults.CookieNoncePrefix));
            Assert.Contains("path=/", nonce);
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
            var correlation = Assert.Single(setCookie.Value, v => v.StartsWith(".AspNetCore.Correlation."));
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
            });

            var server = setting.CreateTestServer();

            var transaction = await server.SendAsync(DefaultHost + TestServerBuilder.Challenge);
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);
            var setCookie = Assert.Single(res.Headers, h => h.Key == "Set-Cookie");
            var correlation = Assert.Single(setCookie.Value, v => v.StartsWith(".AspNetCore.Correlation."));
            Assert.Contains("path=/", correlation);
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
                            nonceTime = DateTime.MaxValue;
                        else
                            nonceTime += nonceLifetime;
                    }
                    catch
                    {
                    }
                }
            }

            return nonceTime;
        }

    }
}
