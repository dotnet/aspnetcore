// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Notifications;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication.OAuthBearer
{
    public class OAuthBearerMiddlewareTests
    {
        [Fact]
        public async Task BearerTokenValidation()
        {
            var server = CreateServer(options =>
            {
                options.Authority = "https://login.windows.net/tushartest.onmicrosoft.com";
                options.Audience = "https://TusharTest.onmicrosoft.com/TodoListService-ManualJwt";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = false
                };
            });
            string newBearerToken = "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6ImtyaU1QZG1Cdng2OHNrVDgtbVBBQjNCc2VlQSJ9.eyJhdWQiOiJodHRwczovL1R1c2hhclRlc3Qub25taWNyb3NvZnQuY29tL1RvZG9MaXN0U2VydmljZS1NYW51YWxKd3QiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZmJlY2UwMy1hZWFhLTRmM2YtODVlNy1jZTA4ZGQyMGNlNTAvIiwiaWF0IjoxNDE4MzMwNjE0LCJuYmYiOjE0MTgzMzA2MTQsImV4cCI6MTQxODMzNDUxNCwidmVyIjoiMS4wIiwidGlkIjoiYWZiZWNlMDMtYWVhYS00ZjNmLTg1ZTctY2UwOGRkMjBjZTUwIiwiYW1yIjpbInB3ZCJdLCJvaWQiOiI1Mzk3OTdjMi00MDE5LTQ2NTktOWRiNS03MmM0Yzc3NzhhMzMiLCJ1cG4iOiJWaWN0b3JAVHVzaGFyVGVzdC5vbm1pY3Jvc29mdC5jb20iLCJ1bmlxdWVfbmFtZSI6IlZpY3RvckBUdXNoYXJUZXN0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6IkQyMm9aMW9VTzEzTUFiQXZrdnFyd2REVE80WXZJdjlzMV9GNWlVOVUwYnciLCJmYW1pbHlfbmFtZSI6Ikd1cHRhIiwiZ2l2ZW5fbmFtZSI6IlZpY3RvciIsImFwcGlkIjoiNjEzYjVhZjgtZjJjMy00MWI2LWExZGMtNDE2Yzk3ODAzMGI3IiwiYXBwaWRhY3IiOiIwIiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwiYWNyIjoiMSJ9.N_Kw1EhoVGrHbE6hOcm7ERdZ7paBQiNdObvp2c6T6n5CE8p0fZqmUd-ya_EqwElcD6SiKSiP7gj0gpNUnOJcBl_H2X8GseaeeMxBrZdsnDL8qecc6_ygHruwlPltnLTdka67s1Ow4fDSHaqhVTEk6lzGmNEcbNAyb0CxQxU6o7Fh0yHRiWoLsT8yqYk8nKzsHXfZBNby4aRo3_hXaa4i0SZLYfDGGYPdttG4vT_u54QGGd4Wzbonv2gjDlllOVGOwoJS6kfl1h8mk0qxdiIaT_ChbDWgkWvTB7bTvBE-EgHgV0XmAo0WtJeSxgjsG3KhhEPsONmqrSjhIUV4IVnF2w";
            var response = await SendAsync(server, "http://example.com/oauth", newBearerToken);
            response.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CustomHeaderReceived()
        {
            var server = CreateServer(options =>
            {
                options.Notifications.MessageReceived = HeaderReceived;
            });

            var response = await SendAsync(server, "http://example.com/oauth", "someHeader someblob");
            response.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private static Task HeaderReceived(MessageReceivedNotification<HttpContext, OAuthBearerAuthenticationOptions> notification)
        {
            List<Claim> claims =
                new List<Claim>
                {
                    new Claim(ClaimTypes.Email, "bob@contoso.com"),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, "bob"),
                };

            notification.AuthenticationTicket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims)), new Http.Authentication.AuthenticationProperties(), notification.Options.AuthenticationScheme);
            notification.HandleResponse();

            return Task.FromResult<object>(null);
        }

        [Fact]
        public async Task NoHeaderReceived()
        {
            var server = CreateServer(options => { });
            var response = await SendAsync(server, "http://example.com/oauth");
            response.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task HeaderWithoutBearerReceived()
        {
            var server = CreateServer(options => { });
            var response = await SendAsync(server, "http://example.com/oauth","Token");
            response.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CustomTokenReceived()
        {
            var server = CreateServer(options =>
            {
                options.Notifications.SecurityTokenReceived = SecurityTokenReceived;
            });

            var response = await SendAsync(server, "http://example.com/oauth", "Bearer someblob");
            response.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private static Task SecurityTokenReceived(SecurityTokenReceivedNotification<HttpContext, OAuthBearerAuthenticationOptions> notification)
        {
            List<Claim> claims =
                new List<Claim>
                {
                    new Claim(ClaimTypes.Email, "bob@contoso.com"),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, "bob"),
                };

            notification.AuthenticationTicket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, notification.Options.AuthenticationScheme)), new Http.Authentication.AuthenticationProperties(), notification.Options.AuthenticationScheme);
            notification.HandleResponse();

            return Task.FromResult<object>(null);
        }

        [Fact]
        public async Task CustomTokenValidated()
        {
            var server = CreateServer(options =>
            {
                options.Notifications.SecurityTokenValidated = SecurityTokenValidated;
                options.SecurityTokenValidators = new List<ISecurityTokenValidator>{new BlobTokenValidator(options.AuthenticationScheme)};
            });

            var response = await SendAsync(server, "http://example.com/oauth", "Bearer someblob");
            response.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private static Task SecurityTokenValidated(SecurityTokenValidatedNotification<HttpContext, OAuthBearerAuthenticationOptions> notification)
        {
            List<Claim> claims =
                new List<Claim>
                {
                    new Claim(ClaimTypes.Email, "bob@contoso.com"),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, "bob"),
                };

            notification.AuthenticationTicket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, notification.Options.AuthenticationScheme)), new Http.Authentication.AuthenticationProperties(), notification.Options.AuthenticationScheme);
            notification.HandleResponse();

            return Task.FromResult<object>(null);
        }

        [Fact]
        public async Task RetrievingTokenFromAlternateLocation()
        {
            var server = CreateServer(options => {
                options.Notifications.MessageReceived = MessageReceived;
                options.Notifications.SecurityTokenReceived = SecurityTokenReceived;
            });
            var response = await SendAsync(server, "http://example.com/oauth", "Bearer Token");
            response.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private static Task MessageReceived(MessageReceivedNotification<HttpContext, OAuthBearerAuthenticationOptions> notification)
        {
            notification.Token = "CustomToken";
            return Task.FromResult<object>(null);
        }

        [Fact]
        public async Task BearerTurns401To403IfAuthenticated()
        {
            var server = CreateServer(options =>
            {
                options.Notifications.SecurityTokenReceived = SecurityTokenReceived;
            });

            var response = await SendAsync(server, "http://example.com/unauthorized", "Bearer Token");
            response.Response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }


        [Fact]
        public async Task BearerDoesNothingTo401IfNotAuthenticated()
        {
            var server = CreateServer(options =>
            {
                options.Notifications.SecurityTokenReceived = SecurityTokenReceived;
            });

            var response = await SendAsync(server, "http://example.com/unauthorized");
            response.Response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        class BlobTokenValidator : ISecurityTokenValidator
        {

            public BlobTokenValidator(string authenticationScheme)
            {
                AuthenticationScheme = authenticationScheme;
            }

            public string AuthenticationScheme { get; set; }

            public bool CanValidateToken
            {
                get
                {
                    return true;
                }
            }

            public int MaximumTokenSizeInBytes
            {
                get
                {
                    return 2*2*1024;
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public bool CanReadToken(string securityToken)
            {
                return true;
            }

            public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
            {
                validatedToken = null;
                List<Claim> claims =
                    new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, "bob@contoso.com"),
                        new Claim(ClaimsIdentity.DefaultNameClaimType, "bob"),
                    };

                return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme));
            }
        }

        private static TestServer CreateServer(Action<OAuthBearerAuthenticationOptions> configureOptions, Func<HttpContext, bool> handler = null)
        {
            return TestServer.Create(app =>
            {
                app.UseServices(services =>
                {
                    services.AddDataProtection();
                });

                if (configureOptions != null)
                {
                    app.UseOAuthBearerAuthentication(configureOptions);
                }
                app.Use(async (context, next) =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    if (req.Path == new PathString("/oauth"))
                    {
                    }
                    else if (req.Path == new PathString("/unauthorized"))
                    {
                        // Simulate Authorization failure 
                        var result = await context.AuthenticateAsync(OAuthBearerAuthenticationDefaults.AuthenticationScheme);
                        res.Challenge(OAuthBearerAuthenticationDefaults.AuthenticationScheme);
                    }
                    else
                    {
                        await next();
                    }

                });
            });
        }

        private static async Task<Transaction> SendAsync(TestServer server, string uri, string authorizationHeader = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                request.Headers.Add("Authorization", authorizationHeader);
            }

            var transaction = new Transaction
            {
                Request = request,
                Response = await server.CreateClient().SendAsync(request),
            };

            transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

            if (transaction.Response.Content != null &&
                transaction.Response.Content.Headers.ContentType != null &&
                transaction.Response.Content.Headers.ContentType.MediaType == "text/xml")
            {
                transaction.ResponseElement = XElement.Parse(transaction.ResponseText);
            }

            return transaction;
        }

        private class Transaction
        {
            public HttpRequestMessage Request { get; set; }
            public HttpResponseMessage Response { get; set; }
            public IList<string> SetCookie { get; set; }
            public string ResponseText { get; set; }
            public XElement ResponseElement { get; set; }
        }
    }
}
