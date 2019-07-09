
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Core.Test
{
    public class AuthenticationSchemeProviderTests
    {
        [Fact]
        public async Task NoDefaultsByDefault()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignInHandler>("B", "whatever");
            }).BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.Null(await provider.GetDefaultForbidSchemeAsync());
            Assert.Null(await provider.GetDefaultAuthenticateSchemeAsync());
            Assert.Null(await provider.GetDefaultChallengeSchemeAsync());
            Assert.Null(await provider.GetDefaultSignInSchemeAsync());
            Assert.Null(await provider.GetDefaultSignOutSchemeAsync());
        }

        [Fact]
        public async Task DefaultSchemesFallbackToDefaultScheme()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.DefaultScheme = "B";
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
        public async Task DefaultSchemesAreSet()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<SignInHandler>("A", "whatever");
                o.AddScheme<SignInHandler>("B", "whatever");
                o.AddScheme<SignInHandler>("C", "whatever");
                o.AddScheme<SignInHandler>("Def", "whatever");
                o.DefaultScheme = "Def";
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

        [Fact]
        public void SchemeRegistrationIsCaseSensitive()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
                o.AddScheme<Handler>("signin", "whatever");
                o.AddScheme<Handler>("signin", "whatever");
            }).BuildServiceProvider();

            var error = Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<IAuthenticationSchemeProvider>());

            Assert.Contains("Scheme already exists: signin", error.Message);
        }

        [Fact]
        public void CanSafelyTryAddSchemes()
        {
            var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
            {
            }).BuildServiceProvider();

            var o = services.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.True(o.TryAddScheme(new AuthenticationScheme("signin", "whatever", typeof(Handler))));
            Assert.True(o.TryAddScheme(new AuthenticationScheme("signin2", "whatever", typeof(Handler))));
            Assert.False(o.TryAddScheme(new AuthenticationScheme("signin", "whatever", typeof(Handler))));
            Assert.True(o.TryAddScheme(new AuthenticationScheme("signin3", "whatever", typeof(Handler))));
            Assert.False(o.TryAddScheme(new AuthenticationScheme("signin2", "whatever", typeof(Handler))));
            o.RemoveScheme("signin2");
            Assert.True(o.TryAddScheme(new AuthenticationScheme("signin2", "whatever", typeof(Handler))));
        }

        [Fact]
        public async Task LookupUsesProvidedStringComparer()
        {
            var services = new ServiceCollection().AddOptions()
                .AddSingleton<IAuthenticationSchemeProvider, IgnoreCaseSchemeProvider>()
                .AddAuthenticationCore(o => o.AddScheme<Handler>("signin", "whatever"))
                .BuildServiceProvider();

            var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();

            var a = await provider.GetSchemeAsync("signin");
            var b = await provider.GetSchemeAsync("SignIn");
            var c = await provider.GetSchemeAsync("SIGNIN");

            Assert.NotNull(a);
            Assert.Same(a, b);
            Assert.Same(b, c);
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

        private class IgnoreCaseSchemeProvider : AuthenticationSchemeProvider
        {
            public IgnoreCaseSchemeProvider(IOptions<AuthenticationOptions> options)
                : base(options, new Dictionary<string, AuthenticationScheme>(StringComparer.OrdinalIgnoreCase))
            {
            }
        }
    }
}
