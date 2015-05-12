// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.Http.Features.Authentication.Internal;
using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Http.Authentication.Internal
{
    public class AuthenticationManagerTests
    {

        [Fact]
        public async Task AuthenticateWithNoAuthMiddlewareThrows()
        {
            var context = CreateContext();
            Assert.Throws<InvalidOperationException>(() => context.Authentication.Authenticate("Foo"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await context.Authentication.AuthenticateAsync("Foo"));
        }

        [Fact]
        public void ChallengeWithNoAuthMiddlewareMayThrow()
        {
            var context = CreateContext();
            context.Authentication.Challenge();
            Assert.Equal(401, context.Response.StatusCode);

            Assert.Throws<InvalidOperationException>(() => context.Authentication.Challenge("Foo"));
        }

        [Fact]
        public void SignInWithNoAuthMiddlewareThrows()
        {
            var context = CreateContext();
            Assert.Throws<InvalidOperationException>(() => context.Authentication.SignIn("Foo", new ClaimsPrincipal()));
        }

        [Fact]
        public void SignOutWithNoAuthMiddlewareMayThrow()
        {
            var context = CreateContext();
            context.Authentication.SignOut();

            Assert.Throws<InvalidOperationException>(() => context.Authentication.SignOut("Foo"));
        }

        [Fact]
        public void SignInOutIn()
        {
            var context = CreateContext();
            var handler = new AuthHandler();
            context.SetFeature<IHttpAuthenticationFeature>(new HttpAuthenticationFeature() { Handler = handler });
            var user = new ClaimsPrincipal();
            context.Authentication.SignIn("ignored", user);
            Assert.True(handler.SignedIn);
            context.Authentication.SignOut("ignored");
            Assert.False(handler.SignedIn);
            context.Authentication.SignIn("ignored", user);
            Assert.True(handler.SignedIn);
            context.Authentication.SignOut("ignored", new AuthenticationProperties() { RedirectUri = "~/logout" });
            Assert.False(handler.SignedIn);
        }

        private class AuthHandler : IAuthenticationHandler
        {
            public bool SignedIn { get; set; }

            public void Authenticate(AuthenticateContext context)
            {
                throw new NotImplementedException();
            }

            public Task AuthenticateAsync(AuthenticateContext context)
            {
                throw new NotImplementedException();
            }

            public void Challenge(ChallengeContext context)
            {
                throw new NotImplementedException();
            }

            public void GetDescriptions(DescribeSchemesContext context)
            {
                throw new NotImplementedException();
            }

            public void SignIn(SignInContext context)
            {
                SignedIn = true;
                context.Accept();
            }

            public void SignOut(SignOutContext context)
            {
                SignedIn = false;
                context.Accept();
            }
        }

        private HttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            return context;
        }
    }
}
