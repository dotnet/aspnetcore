// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class AuthenticationServiceTests
    {
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

            await context.SignInAsync("uber", new ClaimsPrincipal(), null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync("base", new ClaimsPrincipal(), null));
            await context.SignInAsync("signin", new ClaimsPrincipal(), null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync("signout", new ClaimsPrincipal(), null));
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
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync("base"));
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
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
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
            await context.SignInAsync(new ClaimsPrincipal());
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
            await context.SignInAsync(new ClaimsPrincipal());
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
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
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


        private class BaseHandler : IAuthenticationHandler
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
