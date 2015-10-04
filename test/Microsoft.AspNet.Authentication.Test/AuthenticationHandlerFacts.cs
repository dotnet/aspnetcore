// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class AuthenticationHandlerFacts
    {
        [Fact]
        public async Task ShouldHandleSchemeAreDeterminedOnlyByMatchingAuthenticationScheme()
        {
            var handler = await TestHandler.Create("Alpha");
            var passiveNoMatch = handler.ShouldHandleScheme("Beta");

            handler = await TestHandler.Create("Alpha");
            var passiveWithMatch = handler.ShouldHandleScheme("Alpha");

            Assert.False(passiveNoMatch);
            Assert.True(passiveWithMatch);
        }

        [Fact]
        public async Task AutomaticHandlerInAutomaticModeHandlesEmptyChallenges()
        {
            var handler = await TestAutoHandler.Create("ignored", true);
            Assert.True(handler.ShouldHandleScheme(""));
        }

        [Fact]
        public async Task AutomaticHandlerHandlesNullScheme()
        {
            var handler = await TestAutoHandler.Create("ignored", true);
            Assert.True(handler.ShouldHandleScheme(null));
        }

        [Fact]
        public async Task AutomaticHandlerIgnoresWhitespaceScheme()
        {
            var handler = await TestAutoHandler.Create("ignored", true);
            Assert.False(handler.ShouldHandleScheme("    "));
        }

        [Fact]
        public async Task AutomaticHandlerShouldHandleSchemeWhenSchemeMatches()
        {
            var handler = await TestAutoHandler.Create("Alpha", true);
            Assert.True(handler.ShouldHandleScheme("Alpha"));
        }

        [Fact]
        public async Task AutomaticHandlerShouldNotHandleChallengeWhenSchemeDoesNotMatches()
        {
            var handler = await TestAutoHandler.Create("Dog", true);
            Assert.False(handler.ShouldHandleScheme("Alpha"));
        }

        [Fact]
        public async Task AutomaticHandlerShouldNotHandleChallengeWhenSchemesNotEmpty()
        {
            var handler = await TestAutoHandler.Create(null, true);
            Assert.False(handler.ShouldHandleScheme("Alpha"));
        }

        [Theory]
        [InlineData("Alpha")]
        [InlineData("")]
        public async Task AuthHandlerAuthenticateCachesTicket(string scheme)
        {
            var handler = await CountHandler.Create(scheme);
            var context = new AuthenticateContext(scheme);
            await handler.AuthenticateAsync(context);
            await handler.AuthenticateAsync(context);
            Assert.Equal(1, handler.AuthCount);
        }

        private class CountOptions : AuthenticationOptions { }

        private class CountHandler : AuthenticationHandler<CountOptions>
        {
            public int AuthCount = 0;

            private CountHandler() { }

            public static async Task<CountHandler> Create(string scheme)
            {
                var handler = new CountHandler();
                var context = new DefaultHttpContext();
                context.Features.Set<IHttpResponseFeature>(new TestResponse());
                await handler.InitializeAsync(
                    new CountOptions(), context,
                    new LoggerFactory().CreateLogger("CountHandler"),
                    Extensions.WebEncoders.UrlEncoder.Default);
                handler.Options.AuthenticationScheme = scheme;
                handler.Options.AutomaticAuthentication = true;
                return handler;
            }

            protected override Task<AuthenticationTicket> HandleAuthenticateAsync()
            {
                AuthCount++;
                return Task.FromResult(new AuthenticationTicket(null, null));
            }

        }

        private class TestHandler : AuthenticationHandler<TestOptions>
        {
            private TestHandler() { }

            public static async Task<TestHandler> Create(string scheme)
            {
                var handler = new TestHandler();
                var context = new DefaultHttpContext();
                context.Features.Set<IHttpResponseFeature>(new TestResponse());
                await handler.InitializeAsync(
                    new TestOptions(), context,
                    new LoggerFactory().CreateLogger("TestHandler"),
                    Extensions.WebEncoders.UrlEncoder.Default);
                handler.Options.AuthenticationScheme = scheme;
                return handler;
            }

            protected override Task<AuthenticationTicket> HandleAuthenticateAsync()
            {
                return Task.FromResult<AuthenticationTicket>(null);
            }
        }

        private class TestOptions : AuthenticationOptions { }

        private class TestAutoOptions : AuthenticationOptions
        {
            public TestAutoOptions()
            {
                AutomaticAuthentication = true;
            }
        }

        private class TestAutoHandler : AuthenticationHandler<TestAutoOptions>
        {
            private TestAutoHandler() { }

            public static async Task<TestAutoHandler> Create(string scheme, bool auto)
            {
                var handler = new TestAutoHandler();
                var context = new DefaultHttpContext();
                context.Features.Set<IHttpResponseFeature>(new TestResponse());
                await handler.InitializeAsync(
                    new TestAutoOptions(), context,
                    new LoggerFactory().CreateLogger("TestAutoHandler"),
                    Extensions.WebEncoders.UrlEncoder.Default);
                handler.Options.AuthenticationScheme = scheme;
                handler.Options.AutomaticAuthentication = auto;
                return handler;
            }

            protected override Task<AuthenticationTicket> HandleAuthenticateAsync()
            {
                return Task.FromResult<AuthenticationTicket>(null);
            }
        }

        private class TestResponse : IHttpResponseFeature
        {
            public Stream Body
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public bool HasStarted
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IDictionary<string, StringValues> Headers
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public string ReasonPhrase
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public int StatusCode
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public void OnCompleted(Func<object, Task> callback, object state)
            {
                throw new NotImplementedException();
            }

            public void OnStarting(Func<object, Task> callback, object state)
            {
            }
        }
    }
}
