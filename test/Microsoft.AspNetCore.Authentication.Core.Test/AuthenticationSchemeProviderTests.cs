
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
    public class AuthenticationSchemeProviderTests
    {
        [Fact]
        public async Task DefaultSignOutFallsbackToSignIn()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignInHandler>("signin", "whatever");
                o.AddScheme<Handler>("foobly", "whatever");
                o.DefaultSignInScheme = "signin";
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await provider.GetDefaultSignOutSchemeAsync();
            Assert.NotNull(scheme);
            Assert.Equal("signin", scheme.Name);
        }

        [Fact]
        public async Task DefaultForbidFallsbackToChallenge()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<Handler>("challenge", "whatever");
                o.AddScheme<Handler>("foobly", "whatever");
                o.DefaultChallengeScheme = "challenge";
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await provider.GetDefaultForbidSchemeAsync();
            Assert.NotNull(scheme);
            Assert.Equal("challenge", scheme.Name);
        }

        [Fact]
        public async Task DefaultSchemesFallbackToOnlyScheme()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignInHandler>("single", "whatever");
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.Equal("single", (await provider.GetDefaultForbidSchemeAsync()).Name);
            Assert.Equal("single", (await provider.GetDefaultAuthenticateSchemeAsync()).Name);
            Assert.Equal("single", (await provider.GetDefaultChallengeSchemeAsync()).Name);
            Assert.Equal("single", (await provider.GetDefaultSignInSchemeAsync()).Name);
            Assert.Equal("single", (await provider.GetDefaultSignOutSchemeAsync()).Name);
        }

        [Fact]
        public async Task DefaultSchemesFallbackToAuthenticateScheme()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.DefaultAuthenticateScheme = "B";
                o.AddScheme<Handler>("A", "whatever");
                o.AddScheme<SignInHandler>("B", "whatever");
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.Equal("B", (await provider.GetDefaultForbidSchemeAsync()).Name);
            Assert.Equal("B", (await provider.GetDefaultAuthenticateSchemeAsync()).Name);
            Assert.Equal("B", (await provider.GetDefaultChallengeSchemeAsync()).Name);
            Assert.Equal("B", (await provider.GetDefaultSignInSchemeAsync()).Name);
            Assert.Equal("B", (await provider.GetDefaultSignOutSchemeAsync()).Name);
        }

        [Fact]
        public async Task DefaultSchemesAreSet()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignInHandler>("A", "whatever");
                o.AddScheme<SignInHandler>("B", "whatever");
                o.AddScheme<SignInHandler>("C", "whatever");
                o.DefaultChallengeScheme = "A";
                o.DefaultForbidScheme = "B";
                o.DefaultSignInScheme = "C";
                o.DefaultSignOutScheme = "A";
                o.DefaultAuthenticateScheme = "C";
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.Equal("B", (await provider.GetDefaultForbidSchemeAsync()).Name);
            Assert.Equal("C", (await provider.GetDefaultAuthenticateSchemeAsync()).Name);
            Assert.Equal("A", (await provider.GetDefaultChallengeSchemeAsync()).Name);
            Assert.Equal("C", (await provider.GetDefaultSignInSchemeAsync()).Name);
            Assert.Equal("A", (await provider.GetDefaultSignOutSchemeAsync()).Name);
        }

        [Fact]
        public async Task SignInSignOutDefaultsToOnlyOne()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<Handler>("basic", "whatever");
                o.AddScheme<SignOutHandler>("signout", "whatever");
                o.AddScheme<SignInHandler>("signin", "whatever");
                o.DefaultAuthenticateScheme = "basic";
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.Equal("basic", (await provider.GetDefaultForbidSchemeAsync()).Name);
            Assert.Equal("basic", (await provider.GetDefaultAuthenticateSchemeAsync()).Name);
            Assert.Equal("basic", (await provider.GetDefaultChallengeSchemeAsync()).Name);
            Assert.Equal("signin", (await provider.GetDefaultSignInSchemeAsync()).Name);
            Assert.Equal("signin", (await provider.GetDefaultSignOutSchemeAsync()).Name); // Defaults to single sign in scheme
        }

        [Fact]
        public async Task SignOutWillDefaultsToSignInThatDoesNotSignOut()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<Handler>("signin", "whatever");
                o.DefaultSignInScheme = "signin";
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.NotNull(await provider.GetDefaultSignOutSchemeAsync());
        }

        private class Handler : IAuthenticationHandler
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
                throw new NotImplementedException();
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class SignInHandler : Handler, IAuthenticationSignInHandler
        {
            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        private class SignOutHandler : Handler, IAuthenticationSignOutHandler
        {
            public Task SignOutAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }
    }
}
