// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Core.Test
{
    public class AuthenticationServiceTests
    {
        [Fact]
        public async Task AuthenticateThrowsForSchemeMismatch()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<BaseHandler>("base", "whatever");
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.AuthenticateAsync("base");
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.AuthenticateAsync("missing"));
            Assert.Contains("base", ex.Message);
        }

        [Fact]
        public async Task CustomHandlersAuthenticateRunsClaimsTransformationEveryTime()
        {
            var transform = new RunOnce();
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<BaseHandler>("base", "whatever");
            })
                .AddSingleton<IClaimsTransformation>(transform)
                .BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            // Because base handler returns a different principal per call, its run multiple times
            await context.AuthenticateAsync("base");
            Assert.Equal(1, transform.Ran);

            await context.AuthenticateAsync("base");
            Assert.Equal(2, transform.Ran);

            await context.AuthenticateAsync("base");
            Assert.Equal(3, transform.Ran);
        }

        [Fact]
        public async Task ChallengeThrowsForSchemeMismatch()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<BaseHandler>("base", "whatever");
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.ChallengeAsync("base");
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.ChallengeAsync("missing"));
            Assert.Contains("base", ex.Message);
        }

        [Fact]
        public async Task ForbidThrowsForSchemeMismatch()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<BaseHandler>("base", "whatever");
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.ForbidAsync("base");
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.ForbidAsync("missing"));
            Assert.Contains("base", ex.Message);
        }

        [Fact]
        public async Task CanOnlySignInWithIsAuthenticated()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignInHandler>("signin", "whatever");
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync("signin", new ClaimsPrincipal(), null));
            await context.SignInAsync("signin", new ClaimsPrincipal(new ClaimsIdentity("whatever")), null);
        }

        [Fact]
        public async Task CanSignInWithoutIsAuthenticated()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignInHandler>("signin", "whatever");
                o.RequireAuthenticatedSignIn = false;
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.SignInAsync("signin", new ClaimsPrincipal(), null);
            await context.SignInAsync("signin", new ClaimsPrincipal(new ClaimsIdentity("whatever")), null);
        }

        [Fact]
        public async Task CanOnlySignInIfSupported()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<UberHandler>("uber", "whatever");
                o.AddScheme<BaseHandler>("base", "whatever");
                o.AddScheme<SignInHandler>("signin", "whatever");
                o.AddScheme<SignOutHandler>("signout", "whatever");
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.SignInAsync("uber", new ClaimsPrincipal(new ClaimsIdentity("whatever")), null);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync("base", new ClaimsPrincipal(new ClaimsIdentity("whatever")), null));
            Assert.Contains("uber", ex.Message);
            Assert.Contains("signin", ex.Message);
            await context.SignInAsync("signin", new ClaimsPrincipal(new ClaimsIdentity("whatever")), null);
            ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync("signout", new ClaimsPrincipal(new ClaimsIdentity("whatever")), null));
            Assert.Contains("uber", ex.Message);
            Assert.Contains("signin", ex.Message);
        }

        [Fact]
        public async Task CanOnlySignOutIfSupported()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<UberHandler>("uber", "whatever");
                o.AddScheme<BaseHandler>("base", "whatever");
                o.AddScheme<SignInHandler>("signin", "whatever");
                o.AddScheme<SignOutHandler>("signout", "whatever");
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.SignOutAsync("uber");
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync("base"));
            Assert.Contains("uber", ex.Message);
            Assert.Contains("signout", ex.Message);
            await context.SignOutAsync("signout");
            await context.SignOutAsync("signin");
        }

        [Fact]
        public async Task ServicesWithDefaultIAuthenticationHandlerMethodsTest()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<BaseHandler>("base", "whatever");
                o.DefaultScheme = "base";
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.AuthenticateAsync();
            await context.ChallengeAsync();
            await context.ForbidAsync();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            Assert.Contains("cannot be used for SignOutAsync", ex.Message);
            ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever"))));
            Assert.Contains("cannot be used for SignInAsync", ex.Message);
        }

        [Fact]
        public async Task ServicesWithDefaultUberMethodsTest()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<UberHandler>("base", "whatever");
                o.DefaultScheme = "base";
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.AuthenticateAsync();
            await context.ChallengeAsync();
            await context.ForbidAsync();
            await context.SignOutAsync();
            await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever")));
        }

        [Fact]
        public async Task ServicesWithDefaultSignInMethodsTest()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignInHandler>("base", "whatever");
                o.DefaultScheme = "base";
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.AuthenticateAsync();
            await context.ChallengeAsync();
            await context.ForbidAsync();
            await context.SignOutAsync();
            await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever")));
        }

        [Fact]
        public async Task ServicesWithDefaultSignOutMethodsTest()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignOutHandler>("base", "whatever");
                o.DefaultScheme = "base";
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.AuthenticateAsync();
            await context.ChallengeAsync();
            await context.ForbidAsync();
            await context.SignOutAsync();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever"))));
            Assert.Contains("cannot be used for SignInAsync", ex.Message);
        }

        [Fact]
        public async Task ServicesWithDefaultForbidMethod_CallsForbidMethod()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<ForbidHandler>("forbid", "whatever");
                o.DefaultForbidScheme = "forbid";
            }).BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = services;

            await context.ForbidAsync();
        }

        private class RunOnce : IClaimsTransformation
        {
            public int Ran = 0;
            public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
            {
                Ran++;
                return Task.FromResult(new ClaimsPrincipal());
            }
        }

        private class BaseHandler : IAuthenticationHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new ClaimsPrincipal(new ClaimsIdentity("whatever")),
                        new AuthenticationProperties(),
                        "whatever")));
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                return Task.FromResult(0);
            }
        }

        private class SignInHandler : IAuthenticationSignInHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                return Task.FromResult(0);
            }

            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }
        }

        public class SignOutHandler : IAuthenticationSignOutHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                return Task.FromResult(0);
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }
        }

        private class UberHandler : IAuthenticationHandler, IAuthenticationRequestHandler, IAuthenticationSignInHandler, IAuthenticationSignOutHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task<bool> HandleRequestAsync()
            {
                return Task.FromResult(false);
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                return Task.FromResult(0);
            }

            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }
        }

        private class ForbidHandler : IAuthenticationHandler, IAuthenticationRequestHandler, IAuthenticationSignInHandler, IAuthenticationSignOutHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task<bool> HandleRequestAsync()
            {
                throw new NotImplementedException();
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                return Task.FromResult(0);
            }

            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }

    }
}
