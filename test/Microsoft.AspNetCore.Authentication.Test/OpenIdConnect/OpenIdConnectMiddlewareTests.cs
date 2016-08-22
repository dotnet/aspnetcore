// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Tests.OpenIdConnect
{
    public class OpenIdConnectMiddlewareTests
    {
        static string noncePrefix = "OpenIdConnect." + "Nonce.";
        static string nonceDelimiter = ".";
        const string DefaultHost = @"https://example.com";
        const string Logout = "/logout";

        /// <summary>
        /// Tests RedirectForSignOutContext replaces the OpenIdConnectMesssage correctly.
        /// summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task SignOutSettingMessage()
        {
            var setting = new TestSettings(opt =>
            {
                opt.Configuration = new OpenIdConnectConfiguration
                {
                    EndSessionEndpoint = "https://example.com/signout_test/signout_request"
                };
            });

            var server = setting.CreateTestServer();

            var transaction = await TestTransaction.SendAsync(server, DefaultHost + TestServerBuilder.Signout);
            var res = transaction.Response;

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            setting.ValidateSignoutRedirect(transaction.Response.Headers.Location);
        }

        [Fact]
        public async Task SignOutWithDefaultRedirectUri()
        {
            var configuration = TestDefaultValues.CreateDefaultOpenIdConnectConfiguration();
            var server = TestServerBuilder.CreateServer(new OpenIdConnectOptions
            {
                Authority = TestDefaultValues.DefaultAuthority,
                ClientId = "Test Id",
                Configuration = configuration
            });

            var transaction = await TestTransaction.SendAsync(server, DefaultHost + TestServerBuilder.Signout);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal(configuration.EndSessionEndpoint, transaction.Response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SignOutWithCustomRedirectUri()
        {
            var configuration = TestDefaultValues.CreateDefaultOpenIdConnectConfiguration();
            var server = TestServerBuilder.CreateServer(new OpenIdConnectOptions
            {
                Authority = TestDefaultValues.DefaultAuthority,
                ClientId = "Test Id",
                Configuration = configuration,
                PostLogoutRedirectUri = "https://example.com/logout"
            });

            var transaction = await TestTransaction.SendAsync(server, DefaultHost + TestServerBuilder.Signout);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Contains(UrlEncoder.Default.Encode("https://example.com/logout"), transaction.Response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SignOutWith_Specific_RedirectUri_From_Authentication_Properites()
        {
            var configuration = TestDefaultValues.CreateDefaultOpenIdConnectConfiguration();
            var server = TestServerBuilder.CreateServer(new OpenIdConnectOptions
            {
                Authority = TestDefaultValues.DefaultAuthority,
                ClientId = "Test Id",
                Configuration = configuration,
                PostLogoutRedirectUri = "https://example.com/logout"
            });

            var transaction = await TestTransaction.SendAsync(server, "https://example.com/signout_with_specific_redirect_uri");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Contains(UrlEncoder.Default.Encode("http://www.example.com/specific_redirect_uri"), transaction.Response.Headers.Location.AbsoluteUri);
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