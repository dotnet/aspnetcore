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
                o.AddScheme<Handler>("signin", "whatever");
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
                o.AddScheme<Handler>("single", "whatever");
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.Equal("single", (await provider.GetDefaultForbidSchemeAsync()).Name);
            Assert.Equal("single", (await provider.GetDefaultAuthenticateSchemeAsync()).Name);
            Assert.Equal("single", (await provider.GetDefaultChallengeSchemeAsync()).Name);
            Assert.Equal("single", (await provider.GetDefaultSignInSchemeAsync()).Name);
            Assert.Equal("single", (await provider.GetDefaultSignOutSchemeAsync()).Name);
        }

        [Fact]
        public async Task DefaultSchemesAreSet()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<Handler>("A", "whatever");
                o.AddScheme<Handler>("B", "whatever");
                o.AddScheme<Handler>("C", "whatever");
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
