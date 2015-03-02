// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Http.Core;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class AuthenticationHandlerFacts
    {
        [Fact]
        public void ShouldHandleSchemeAreDeterminedOnlyByMatchingAuthenticationScheme()
        {
            var handler = new TestHandler("Alpha");

            bool passiveNoMatch = handler.ShouldHandleScheme(new[] { "Beta", "Gamma" });

            handler = new TestHandler("Alpha");

            bool passiveWithMatch = handler.ShouldHandleScheme(new[] { "Beta", "Alpha" });

            Assert.False(passiveNoMatch);
            Assert.True(passiveWithMatch);
        }

        [Fact]
        public void AutomaticHandlerInAutomaticModeHandlesEmptyChallenges()
        {
            var handler = new TestAutoHandler("ignored", true);
            Assert.True(handler.ShouldHandleScheme(new string[0]));
        }

        [Fact]
        public void AutomaticHandlerShouldHandleSchemeWhenSchemeMatches()
        {
            var handler = new TestAutoHandler("Alpha", true);
            Assert.True(handler.ShouldHandleScheme(new string[] { "Alpha" }));
        }

        [Fact]
        public void AutomaticHandlerShouldNotHandleChallengeWhenSchemeDoesNotMatches()
        {
            var handler = new TestAutoHandler("Dog", true);
            Assert.False(handler.ShouldHandleScheme(new string[] { "Alpha" }));
        }

        [Fact]
        public void AutomaticHandlerShouldNotHandleChallengeWhenSchemesNotEmpty()
        {
            var handler = new TestAutoHandler(null, true);
            Assert.False(handler.ShouldHandleScheme(new string[] { "Alpha" }));
        }

        private class TestHandler : AuthenticationHandler<AuthenticationOptions>
        {
            public TestHandler(string scheme)
            {
                Initialize(new TestOptions(), new DefaultHttpContext());
                Options.AuthenticationScheme = scheme;
            }

            protected override void ApplyResponseChallenge()
            {
                throw new NotImplementedException();
            }

            protected override void ApplyResponseGrant()
            {
                throw new NotImplementedException();
            }

            protected override AuthenticationTicket AuthenticateCore()
            {
                throw new NotImplementedException();
            }
        }

        private class TestOptions : AuthenticationOptions { }

        private class TestAutoOptions : AutomaticAuthenticationOptions { }

        private class TestAutoHandler : AutomaticAuthenticationHandler<AutomaticAuthenticationOptions>
        {
            public TestAutoHandler(string scheme, bool auto)
            {
                Initialize(new TestAutoOptions(), new DefaultHttpContext());
                Options.AuthenticationScheme = scheme;
                Options.AutomaticAuthentication = auto;
            }

            protected override void ApplyResponseChallenge()
            {
                throw new NotImplementedException();
            }

            protected override void ApplyResponseGrant()
            {
                throw new NotImplementedException();
            }

            protected override AuthenticationTicket AuthenticateCore()
            {
                throw new NotImplementedException();
            }
        }

    }
}
