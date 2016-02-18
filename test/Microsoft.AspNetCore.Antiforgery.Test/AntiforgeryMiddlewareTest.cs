// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication.Internal;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Antiforgery
{
    // These are really more like integration tests and just verify a bunch of different
    // reasonable combinations of authN middleware.
    public class AntiforgeryMiddlewareTest
    {
        private readonly ClaimsPrincipal LoggedInUser = new ClaimsPrincipal(new ClaimsIdentity[]
        {
            new ClaimsIdentity("Test"),
        });

        private readonly ClaimsPrincipal LoggedInUser2 = new ClaimsPrincipal(new ClaimsIdentity[]
        {
            new ClaimsIdentity("Test"),
        });

        [Fact]
        public async Task AutomaticAuthentication_Anonymous()
        {
            // Arrange
            var context = Setup((app) =>
            {
                app.Use(next => new AutomaticAuthenticationMiddleware(next, null).Invoke);
                app.UseAntiforgery();
            });

            var httpContext = new DefaultHttpContext();

            await context.AppFunc(httpContext);

            Assert.Null(context.Principal);
        }

        [Fact]
        public async Task AutomaticAuthentication_LoggedIn_WithoutToken()
        {
            // Arrange
            var context = Setup((app) =>
            {
                app.UseMiddleware<AutomaticAuthenticationMiddleware>(LoggedInUser);
                app.UseAntiforgery();
            });

            context.Antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false);

            var httpContext = new DefaultHttpContext();

            await context.AppFunc(httpContext);

            Assert.Null(context.Principal);
        }

        [Fact]
        public async Task AutomaticAuthentication_LoggedIn_WithValidToken()
        {
            // Arrange
            var context = Setup((app) =>
            {
                app.UseMiddleware<AutomaticAuthenticationMiddleware>(LoggedInUser);
                app.UseAntiforgery();
            });

            context.Antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(true);

            var httpContext = new DefaultHttpContext();

            await context.AppFunc(httpContext);

            Assert.Same(LoggedInUser, context.Principal);
        }

        // A middleware after antiforgery in the pipeline can authenticate without going through token
        // validation.
        [Fact]
        public async Task AutomaticAuthentication_LoggedIn_WithoutToken_AuthenticatedBySubsequentMiddleware()
        {
            // Arrange
            var context = Setup((app) =>
            {
                app.UseMiddleware<AutomaticAuthenticationMiddleware>(LoggedInUser);
                app.UseAntiforgery();
                app.UseMiddleware<AutomaticAuthenticationMiddleware>(LoggedInUser2);
            });

            context.Antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false);

            var httpContext = new DefaultHttpContext();

            await context.AppFunc(httpContext);

            Assert.Same(LoggedInUser2, context.Principal);
        }

        [Fact]
        public async Task PasiveAuthentication_Anonymous()
        {
            // Arrange
            var context = Setup((app) =>
            {
                app.Use(next => new AuthenticationHandlerMiddleware(next, null).Invoke);
                app.UseAntiforgery();
                app.UseMiddleware<CallAuthenticateMiddleware>();
            });

            var httpContext = new DefaultHttpContext();

            await context.AppFunc(httpContext);

            Assert.Null(context.Principal);
        }

        [Fact]
        public async Task PassiveAuthentication_LoggedIn_WithoutToken()
        {
            // Arrange
            var context = Setup((app) =>
            {
                app.UseMiddleware<AuthenticationHandlerMiddleware>(LoggedInUser);
                app.UseAntiforgery();
                app.UseMiddleware<CallAuthenticateMiddleware>();
            });

            context.Antiforgery
                .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), LoggedInUser))
                .Throws(new AntiforgeryValidationException("error"));

            var httpContext = new DefaultHttpContext();

            await context.AppFunc(httpContext);

            Assert.Null(context.Principal);
        }

        [Fact]
        public async Task PassiveAuthentication_LoggedIn_WithValidToken()
        {
            // Arrange
            var context = Setup((app) =>
            {
                app.UseMiddleware<AuthenticationHandlerMiddleware>(LoggedInUser);
                app.UseAntiforgery();
                app.UseMiddleware<CallAuthenticateMiddleware>();
            });

            context.Antiforgery
                .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), LoggedInUser))
                .Returns(TaskCache.CompletedTask);

            var httpContext = new DefaultHttpContext();

            await context.AppFunc(httpContext);

            Assert.Same(LoggedInUser, context.Principal);
        }

        // A middleware after antiforgery in the pipeline can authenticate without going through token
        // validation.
        [Fact]
        public async Task PassiveAuthentication_LoggedIn_WithoutToken_AuthenticatedBySubsequentMiddleware()
        {
            // Arrange
            var context = Setup((app) =>
            {
                app.UseMiddleware<AuthenticationHandlerMiddleware>(LoggedInUser);
                app.UseAntiforgery();
                app.UseMiddleware<AuthenticationHandlerMiddleware>(LoggedInUser2);
                app.UseMiddleware<CallAuthenticateMiddleware>();
            });

            var httpContext = new DefaultHttpContext();

            await context.AppFunc(httpContext);

            Assert.Same(LoggedInUser2, context.Principal);
        }

        private static IHttpAuthenticationFeature GetAuthenticationFeature(HttpContext httpContext)
        {
            var authentication = httpContext.Features.Get<IHttpAuthenticationFeature>();
            if (authentication == null)
            {
                authentication = new HttpAuthenticationFeature();
                httpContext.Features.Set(authentication);
            }

            return authentication;
        }

        private static TestContext Setup(Action<IApplicationBuilder> action)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();

            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            services.AddSingleton(antiforgery.Object);

            var result = new TestContext();
            result.Antiforgery = antiforgery;

            var app = new ApplicationBuilder(services.BuildServiceProvider());
            action(app);

            // Capture the logged in user 'after' the middleware so we can validate it.
            app.Run(c =>
            {
                result.Principal = GetAuthenticationFeature(c).User;
                return TaskCache.CompletedTask;
            });

            result.AppFunc = app.Build();
            return result;
        }

        private class TestContext
        {
            public Mock<IAntiforgery> Antiforgery { get; set; }

            public RequestDelegate AppFunc { get; set; }

            public ClaimsPrincipal Principal { get; set; }
        }

        private class AutomaticAuthenticationMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly ClaimsPrincipal _principal;

            public AutomaticAuthenticationMiddleware(RequestDelegate next, ClaimsPrincipal principal)
            {
                _next = next;
                _principal = principal;
            }

            public Task Invoke(HttpContext httpContext)
            {
                GetAuthenticationFeature(httpContext).User = _principal;
                return _next(httpContext);
            }
        }

        private class AuthenticationHandlerMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly ClaimsPrincipal _principal;

            public AuthenticationHandlerMiddleware(RequestDelegate next, ClaimsPrincipal principal)
            {
                _next = next;
                _principal = principal;
            }

            public async Task Invoke(HttpContext httpContext)
            {
                var handler = new AuthenticationHandler(_principal);
                await handler.InitializeAsync(httpContext);

                try
                {
                    await _next(httpContext);
                }
                finally
                {
                    await handler.TeardownAsync();
                }
            }
        }

        private class AuthenticationHandler : IAuthenticationHandler
        {
            private readonly ClaimsPrincipal _principal;
            private IAuthenticationHandler _priorHandler;
            private HttpContext _httpContext;

            public AuthenticationHandler(ClaimsPrincipal principal)
            {
                _principal = principal;
            }

            public Task InitializeAsync(HttpContext httpContext)
            {
                _httpContext = httpContext;

                var authenticationFeature = GetAuthenticationFeature(_httpContext);
                _priorHandler = authenticationFeature.Handler;
                authenticationFeature.Handler = this;

                return TaskCache.CompletedTask;
            }

            public Task TeardownAsync()
            {
                var authenticationFeature = GetAuthenticationFeature(_httpContext);
                authenticationFeature.Handler = _priorHandler;

                return TaskCache.CompletedTask;
            }

            public Task AuthenticateAsync(AuthenticateContext context)
            {
                if (_principal == null)
                {
                    context.NotAuthenticated();
                }
                else
                {
                    context.Authenticated(_principal, null, null);
                }

                return TaskCache.CompletedTask;
            }

            public Task ChallengeAsync(ChallengeContext context)
            {
                throw new NotImplementedException();
            }

            public void GetDescriptions(DescribeSchemesContext context)
            {
                throw new NotImplementedException();
            }

            public Task SignInAsync(SignInContext context)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(SignOutContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class CallAuthenticateMiddleware
        {
            private readonly RequestDelegate _next;

            public CallAuthenticateMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            public async Task Invoke(HttpContext httpContext)
            {
                var authenticationFeature = GetAuthenticationFeature(httpContext);

                var authenticateContext = new AuthenticateContext("Test");
                await httpContext.Authentication.AuthenticateAsync(authenticateContext);

                if (authenticateContext.Accepted)
                {
                    authenticationFeature.User = authenticateContext.Principal;
                }

                await _next(httpContext);
            }
        }
    }
}
