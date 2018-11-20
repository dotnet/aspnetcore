// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma warning disable CS0618 // Type or member is obsolete
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Xunit;

namespace Microsoft.AspNetCore.Http.Authentication.Internal
{
    public class DefaultAuthenticationManagerTests
    {

        [Fact]
        public async Task AuthenticateWithNoAuthMiddlewareThrows()
        {
            var context = CreateContext();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await context.Authentication.AuthenticateAsync("Foo"));
        }

        [Theory]
        [InlineData("Automatic")]
        [InlineData("Foo")]
        public async Task ChallengeWithNoAuthMiddlewareMayThrow(string scheme)
        {
            var context = CreateContext();
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.Authentication.ChallengeAsync(scheme));
        }

        [Fact]
        public async Task SignInWithNoAuthMiddlewareThrows()
        {
            var context = CreateContext();
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.Authentication.SignInAsync("Foo", new ClaimsPrincipal()));
        }

        [Fact]
        public async Task SignOutWithNoAuthMiddlewareMayThrow()
        {
            var context = CreateContext();
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.Authentication.SignOutAsync("Foo"));
        }

        [Fact]
        public async Task SignInOutIn()
        {
            var context = CreateContext();
            var handler = new AuthHandler();
            context.Features.Set<IHttpAuthenticationFeature>(new HttpAuthenticationFeature() { Handler = handler });
            var user = new ClaimsPrincipal();
            await context.Authentication.SignInAsync("ignored", user);
            Assert.True(handler.SignedIn);
            await context.Authentication.SignOutAsync("ignored");
            Assert.False(handler.SignedIn);
            await context.Authentication.SignInAsync("ignored", user);
            Assert.True(handler.SignedIn);
            await context.Authentication.SignOutAsync("ignored", new AuthenticationProperties() { RedirectUri = "~/logout" });
            Assert.False(handler.SignedIn);
        }

        private class AuthHandler : IAuthenticationHandler
        {
            public bool SignedIn { get; set; }

            public Task AuthenticateAsync(AuthenticateContext context)
            {
                throw new NotImplementedException();
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
                SignedIn = true;
                context.Accept();
                return Task.FromResult(0);
            }

            public Task SignOutAsync(SignOutContext context)
            {
                SignedIn = false;
                context.Accept();
                return Task.FromResult(0);
            }
        }

        private HttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            return context;
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
