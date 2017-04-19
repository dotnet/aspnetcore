// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect
{
    public class OpenIdConnectChallengeTests
    {
        private static readonly string ChallengeEndpoint = TestServerBuilder.TestHost + TestServerBuilder.Challenge;

        [Fact]
        public async Task ChallengeIsIssuedCorrectly()
        {
            var settings = new TestSettings(
                opt =>
                {
                    opt.Authority = TestServerBuilder.DefaultAuthority;
                    opt.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                    opt.ClientId = "Test Id";
                });

            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            settings.ValidateChallengeRedirect(
                res.Headers.Location,
                OpenIdConnectParameterNames.ClientId,
                OpenIdConnectParameterNames.ResponseType,
                OpenIdConnectParameterNames.ResponseMode,
                OpenIdConnectParameterNames.Scope,
                OpenIdConnectParameterNames.RedirectUri,
                OpenIdConnectParameterNames.SkuTelemetry,
                OpenIdConnectParameterNames.VersionTelemetry);
        }

        [Fact]
        public async Task AuthorizationRequestDoesNotIncludeTelemetryParametersWhenDisabled()
        {
            var setting = new TestSettings(opt =>
            {
                opt.ClientId = "Test Id";
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.DisableTelemetry = true;
            });

            var server = setting.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.DoesNotContain(OpenIdConnectParameterNames.SkuTelemetry, res.Headers.Location.Query);
            Assert.DoesNotContain(OpenIdConnectParameterNames.VersionTelemetry, res.Headers.Location.Query);
        }

        /*
        Example of a form post
        <body>
            <form name=\ "form\" method=\ "post\" action=\ "https://login.microsoftonline.com/common/oauth2/authorize\">
                <input type=\ "hidden\" name=\ "client_id\" value=\ "51e38103-238f-410f-a5d5-61991b203e50\" />
                <input type=\ "hidden\" name=\ "redirect_uri\" value=\ "https://example.com/signin-oidc\" />
                <input type=\ "hidden\" name=\ "response_type\" value=\ "id_token\" />
                <input type=\ "hidden\" name=\ "scope\" value=\ "openid profile\" />
                <input type=\ "hidden\" name=\ "response_mode\" value=\ "form_post\" />
                <input type=\ "hidden\" name=\ "nonce\" value=\ "636072461997914230.NTAwOGE1MjQtM2VhYS00ZDU0LWFkYzYtNmZiYWE2MDRkODg3OTlkMDFmOWUtOTMzNC00ZmI2LTg1Y2YtOWM4OTlhNjY0Yjli\" />
                <input type=\ "hidden\" name=\ "state\" value=\
                    "CfDJ8Jh1NKaF0T5AnK4qsqzzIs89srKe4iEaBWd29MNph4Ki887QKgkD24wjhZ0ciH-ar6A_jUmRI2O5haXN2-YXbC0ZRuRAvNsx5LqbPTdh4MJBIwXWkG_rM0T0tI3h5Y2pDttWSaku6a_nzFLUYBrKfsE7sDLVoTDrzzOcHrRQhdztqOOeNUuu2wQXaKwlOtNI21ShtN9EVxvSGFOxUUOwVih4nFdF40fBcbsuPpcpCPkLARQaFRJSYsNKiP7pcFMnRwzZhnISHlyGKkzwJ1DIx7nsmdiQFBGljimw5GnYAs-5ru9L3w8NnPjkl96OyQ8MJOcayMDmOY26avs2sYP_Zw0\" />
                <noscript>Click here to finish the process: <input type=\"submit\" /></noscript>
            </form>
            <script>
                document.form.submit();
            </script>
        </body>
        */
        [Fact]
        public async Task ChallengeIssueedCorrectlyForFormPost()
        {
            var settings = new TestSettings(
                opt =>
                {
                    opt.Authority = TestServerBuilder.DefaultAuthority;
                    opt.AuthenticationMethod = OpenIdConnectRedirectBehavior.FormPost;
                    opt.ClientId = "Test Id";
                });

            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("text/html", transaction.Response.Content.Headers.ContentType.MediaType);

            var body = await res.Content.ReadAsStringAsync();
            settings.ValidateChallengeFormPost(
                body,
                OpenIdConnectParameterNames.ClientId,
                OpenIdConnectParameterNames.ResponseType,
                OpenIdConnectParameterNames.ResponseMode,
                OpenIdConnectParameterNames.Scope,
                OpenIdConnectParameterNames.RedirectUri);
        }

        [Theory]
        [InlineData("sample_user_state")]
        [InlineData(null)]
        public async Task ChallengeCanSetUserStateThroughProperties(string userState)
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("OIDCTest"));
            var settings = new TestSettings(o =>
            {
                o.ClientId = "Test Id";
                o.Authority = TestServerBuilder.DefaultAuthority;
                o.StateDataFormat = stateFormat;
            });

            var properties = new AuthenticationProperties();
            properties.Items.Add(OpenIdConnectDefaults.UserstatePropertiesKey, userState);

            var server = settings.CreateTestServer(properties);
            var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            var values = settings.ValidateChallengeRedirect(res.Headers.Location);
            var actualState = values[OpenIdConnectParameterNames.State];
            var actualProperties = stateFormat.Unprotect(actualState);

            Assert.Equal(userState ?? string.Empty, actualProperties.Items[OpenIdConnectDefaults.UserstatePropertiesKey]);
        }

        [Theory]
        [InlineData("sample_user_state")]
        [InlineData(null)]
        public async Task OnRedirectToIdentityProviderEventCanSetState(string userState)
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("OIDCTest"));
            var settings = new TestSettings(opt =>
            {
                opt.StateDataFormat = stateFormat;
                opt.ClientId = "Test Id";
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.State = userState;
                        return Task.FromResult(0);
                    }
                };
            });

            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            var values = settings.ValidateChallengeRedirect(res.Headers.Location);
            var actualState = values[OpenIdConnectParameterNames.State];
            var actualProperties = stateFormat.Unprotect(actualState);

            if (userState != null)
            {
                Assert.Equal(userState, actualProperties.Items[OpenIdConnectDefaults.UserstatePropertiesKey]);
            }
            else
            {
                Assert.False(actualProperties.Items.ContainsKey(OpenIdConnectDefaults.UserstatePropertiesKey));
            }
        }

        [Fact]
        public async Task OnRedirectToIdentityProviderEventIsHit()
        {
            var eventIsHit = false;
            var settings = new TestSettings(
                opts =>
                {
                    opts.ClientId = "Test Id";
                    opts.Authority = TestServerBuilder.DefaultAuthority;
                    opts.Events = new OpenIdConnectEvents()
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            eventIsHit = true;
                            return Task.FromResult(0);
                        }
                    };
                }
            );

            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            Assert.True(eventIsHit);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            settings.ValidateChallengeRedirect(
                res.Headers.Location,
                OpenIdConnectParameterNames.ClientId,
                OpenIdConnectParameterNames.ResponseType,
                OpenIdConnectParameterNames.ResponseMode,
                OpenIdConnectParameterNames.Scope,
                OpenIdConnectParameterNames.RedirectUri);
        }


        [Fact]
        public async Task OnRedirectToIdentityProviderEventCanReplaceValues()
        {
            var newClientId = Guid.NewGuid().ToString();

            var settings = new TestSettings(
                opts =>
                {
                    opts.ClientId = "Test Id";
                    opts.Authority = TestServerBuilder.DefaultAuthority;
                    opts.Events = new OpenIdConnectEvents()
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            context.ProtocolMessage.ClientId = newClientId;
                            return Task.FromResult(0);
                        }
                    };
                }
            );

            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            settings.ValidateChallengeRedirect(
                res.Headers.Location,
                OpenIdConnectParameterNames.ResponseType,
                OpenIdConnectParameterNames.ResponseMode,
                OpenIdConnectParameterNames.Scope,
                OpenIdConnectParameterNames.RedirectUri);

            var actual = res.Headers.Location.Query.Trim('?').Split('&').Single(seg => seg.StartsWith($"{OpenIdConnectParameterNames.ClientId}="));
            Assert.Equal($"{OpenIdConnectParameterNames.ClientId}={newClientId}", actual);
        }

        [Fact]
        public async Task OnRedirectToIdentityProviderEventCanReplaceMessage()
        {
            var newMessage = new MockOpenIdConnectMessage
            {
                IssuerAddress = "http://example.com/",
                TestAuthorizeEndpoint = $"http://example.com/{Guid.NewGuid()}/oauth2/signin"
            };

            var settings = new TestSettings(
                opts =>
                {
                    opts.ClientId = "Test Id";
                    opts.Authority = TestServerBuilder.DefaultAuthority;
                    opts.Events = new OpenIdConnectEvents()
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            context.ProtocolMessage = newMessage;

                            return Task.FromResult(0);
                        }
                    };
                }
            );

            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.NotNull(res.Headers.Location);

            // The CreateAuthenticationRequestUrl method is overridden MockOpenIdConnectMessage where
            // query string is not generated and the authorization endpoint is replaced.
            Assert.Equal(newMessage.TestAuthorizeEndpoint, res.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task OnRedirectToIdentityProviderEventHandlesResponse()
        {
            var settings = new TestSettings(
                opts =>
                {
                    opts.ClientId = "Test Id";
                    opts.Authority = TestServerBuilder.DefaultAuthority;
                    opts.Events = new OpenIdConnectEvents()
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            context.Response.StatusCode = 410;
                            context.Response.Headers.Add("tea", "Oolong");
                            context.HandleResponse();

                            return Task.FromResult(0);
                        }
                    };
                }
            );

            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.Gone, res.StatusCode);
            Assert.Equal("Oolong", res.Headers.GetValues("tea").Single());
            Assert.Null(res.Headers.Location);
        }

        // This test can be further refined. When one auth handler skips, the authentication responsibility
        // will be flowed to the next one. A dummy auth handler can be added to ensure the correct logic.
        [Fact]
        public async Task OnRedirectToIdentityProviderEventSkipResponse()
        {
            var settings = new TestSettings(
                opts =>
                {
                    opts.ClientId = "Test Id";
                    opts.Authority = TestServerBuilder.DefaultAuthority;
                    opts.Events = new OpenIdConnectEvents()
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            context.Skip();
                            return Task.FromResult(0);
                        }
                    };
                }
            );

            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var res = transaction.Response;
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Null(res.Headers.Location);
        }

        [Fact]
        public async Task ChallengeSetsNonceAndStateCookies()
        {
            var settings = new TestSettings(o =>
            {
                o.ClientId = "Test Id";
                o.Authority = TestServerBuilder.DefaultAuthority;
            });
            var server = settings.CreateTestServer();
            var transaction = await server.SendAsync(ChallengeEndpoint);

            var firstCookie = transaction.SetCookie.First();
            Assert.Contains(OpenIdConnectDefaults.CookieNoncePrefix, firstCookie);
            Assert.Contains("expires", firstCookie);

            var secondCookie = transaction.SetCookie.Skip(1).First();
            Assert.StartsWith(".AspNetCore.Correlation.OpenIdConnect.", secondCookie);
            Assert.Contains("expires", secondCookie);
        }

        [Fact]
        public async Task Challenge_WithEmptyConfig_Fails()
        {
            var settings = new TestSettings(
                opt =>
                {
                    opt.ClientId = "Test Id";
                    opt.Configuration = new OpenIdConnectConfiguration();
                });

            var server = settings.CreateTestServer();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync(ChallengeEndpoint));
            Assert.Equal("Cannot redirect to the authorization endpoint, the configuration may be missing or invalid.", exception.Message);
        }
    }
}