// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureAD.Controllers.Internal
{
    public class AccountControllerTests
    {
        [Fact]
        public void SignInNoScheme_ChallengesAADAzureADDefaultScheme()
        {
            // Arrange
            var controller = new AccountController(
                new OptionsMonitor(AzureADDefaults.AuthenticationScheme, new AzureADOptions()
                {
                    OpenIdConnectSchemeName = AzureADDefaults.OpenIdScheme,
                    CookieSchemeName = AzureADDefaults.CookieScheme
                }))
            {
                Url = new TestUrlHelper("~/", "https://localhost/")
            };

            // Act
            var result = controller.SignIn(null);

            // Assert
            var challenge = Assert.IsAssignableFrom<ChallengeResult>(result);
            var challengedScheme = Assert.Single(challenge.AuthenticationSchemes);
            Assert.Equal(AzureADDefaults.AuthenticationScheme, challengedScheme);
            Assert.NotNull(challenge.Properties.RedirectUri);
            Assert.Equal("https://localhost/", challenge.Properties.RedirectUri);
        }

        [Fact]
        public void SignInProvidedScheme_ChallengesCustomScheme()
        {
            // Arrange
            var controller = new AccountController(new OptionsMonitor("Custom", new AzureADOptions()));
            controller.Url = new TestUrlHelper("~/", "https://localhost/");

            // Act
            var result = controller.SignIn("Custom");

            // Assert
            var challenge = Assert.IsAssignableFrom<ChallengeResult>(result);
            var challengedScheme = Assert.Single(challenge.AuthenticationSchemes);
            Assert.Equal("Custom", challengedScheme);
        }

        private ClaimsPrincipal CreateAuthenticatedPrincipal(string scheme) =>
            new ClaimsPrincipal(new ClaimsIdentity(scheme));

        private static ControllerContext CreateControllerContext(ClaimsPrincipal principal = null)
        {
            principal = principal ?? new ClaimsPrincipal(new ClaimsIdentity());
            var mock = new Mock<IAuthenticationService>();
            mock.Setup(authS => authS.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .ReturnsAsync<HttpContext, string, IAuthenticationService, AuthenticateResult>(
                    (ctx, scheme) =>
                    {
                        if (principal.Identity.IsAuthenticated)
                        {
                            return AuthenticateResult.Success(new AuthenticationTicket(principal, scheme));
                        }
                        else
                        {
                            return AuthenticateResult.NoResult();
                        }
                    });
            return new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    RequestServices = new ServiceCollection()
                        .AddSingleton(mock.Object)
                        .BuildServiceProvider()
                }
            };
        }

        [Fact]
        public void SignOutNoScheme_SignsOutDefaultCookiesAndDefaultOpenIDConnectAADAzureADSchemesAsync()
        {
            // Arrange
            var options = new AzureADOptions()
            {
                CookieSchemeName = AzureADDefaults.CookieScheme,
                OpenIdConnectSchemeName = AzureADDefaults.OpenIdScheme
            };

            var controllerContext = CreateControllerContext(
                CreateAuthenticatedPrincipal(AzureADDefaults.AuthenticationScheme));

            var descriptor = new PageActionDescriptor()
            {
                AttributeRouteInfo = new AttributeRouteInfo()
                {
                    Template = "/Account/SignedOut"
                }
            };
            var controller = new AccountController(new OptionsMonitor(AzureADDefaults.AuthenticationScheme, options))
            {
                Url = new TestUrlHelper(
                    controllerContext.HttpContext,
                    new RouteData(),
                    descriptor,
                    "/Account/SignedOut",
                    "https://localhost/Account/SignedOut"),
                ControllerContext = new ControllerContext()
                {
                    HttpContext = controllerContext.HttpContext
                }
            };
            controller.Request.Scheme = "https";

            // Act
            var result = controller.SignOut(null);

            // Assert
            var signOut = Assert.IsAssignableFrom<SignOutResult>(result);
            Assert.Equal(new[] { AzureADDefaults.CookieScheme, AzureADDefaults.OpenIdScheme }, signOut.AuthenticationSchemes);
            Assert.NotNull(signOut.Properties.RedirectUri);
            Assert.Equal("https://localhost/Account/SignedOut", signOut.Properties.RedirectUri);
        }

        [Fact]
        public void SignOutProvidedScheme_SignsOutCustomCookiesAndCustomOpenIDConnectAADAzureADSchemesAsync()
        {
            // Arrange
            var options = new AzureADOptions()
            {
                CookieSchemeName = "Cookie",
                OpenIdConnectSchemeName = "OpenID"
            };

            var controllerContext = CreateControllerContext(
                CreateAuthenticatedPrincipal(AzureADDefaults.AuthenticationScheme));
            var descriptor = new PageActionDescriptor()
            {
                AttributeRouteInfo = new AttributeRouteInfo()
                {
                    Template = "/Account/SignedOut"
                }
            };

            var controller = new AccountController(new OptionsMonitor("Custom", options))
            {
                Url = new TestUrlHelper(
                    controllerContext.HttpContext,
                    new RouteData(),
                    descriptor,
                    "/Account/SignedOut",
                    "https://localhost/Account/SignedOut"),
                ControllerContext = new ControllerContext()
                {
                    HttpContext = controllerContext.HttpContext
                }
            };
            controller.Request.Scheme = "https";

            // Act
            var result = controller.SignOut("Custom");

            // Assert
            var signOut = Assert.IsAssignableFrom<SignOutResult>(result);
            Assert.Equal(new[] { "Cookie", "OpenID" }, signOut.AuthenticationSchemes);
        }

        private class OptionsMonitor : IOptionsMonitor<AzureADOptions>
        {
            public OptionsMonitor(string scheme, AzureADOptions options)
            {
                Scheme = scheme;
                Options = options;
            }

            public AzureADOptions CurrentValue => throw new NotImplementedException();

            public string Scheme { get; }
            public AzureADOptions Options { get; }

            public AzureADOptions Get(string name)
            {
                if (name == Scheme)
                {
                    return Options;
                }

                return null;
            }

            public IDisposable OnChange(Action<AzureADOptions, string> listener)
            {
                throw new NotImplementedException();
            }
        }

        private class TestUrlHelper : IUrlHelper
        {
            public TestUrlHelper(string contentPath, string url)
            {
                ContentPath = contentPath;
                Url = url;
            }

            public TestUrlHelper(
                HttpContext context,
                RouteData routeData,
                ActionDescriptor descriptor,
                string contentPath,
                string url)
            {
                HttpContext = context;
                RouteData = routeData;
                ActionDescriptor = descriptor;
                ContentPath = contentPath;
                Url = url;
            }

            public ActionContext ActionContext =>
                new ActionContext(HttpContext, RouteData, ActionDescriptor);

            public string ContentPath { get; }
            public string Url { get; }
            public HttpContext HttpContext { get; }
            public RouteData RouteData { get; }
            public ActionDescriptor ActionDescriptor { get; }

            public string Action(UrlActionContext actionContext)
            {
                throw new NotImplementedException();
            }

            public string Content(string contentPath)
            {
                if (ContentPath == contentPath)
                {
                    return Url;
                }
                return "";
            }

            public bool IsLocalUrl(string url)
            {
                throw new NotImplementedException();
            }

            public string Link(string routeName, object values)
            {
                throw new NotImplementedException();
            }

            public string RouteUrl(UrlRouteContext routeContext)
            {
                if (routeContext.Values is RouteValueDictionary dicionary &&
                    dicionary.TryGetValue("page", out var page) &&
                    page is string pagePath &&
                    ContentPath == pagePath)
                {
                    return Url;
                }

                return null;
            }
        }
    }
}
