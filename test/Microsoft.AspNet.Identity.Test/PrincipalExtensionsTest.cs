// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class ClaimsIdentityExtensionsTest
    {
        public const string ExternalAuthenticationScheme = "TestExternalAuth";

        [Fact]
        public void IdentityNullCheckTest()
        {
            IPrincipal p = null;
            Assert.Throws<ArgumentNullException>("principal", () => p.GetUserId());
            Assert.Throws<ArgumentNullException>("principal", () => p.GetUserName());
            ClaimsPrincipal cp = null;
            Assert.Throws<ArgumentNullException>("principal", () => cp.FindFirstValue(null));
        }

        [Fact]
        public void IdentityNullIfNotClaimsIdentityTest()
        {
            IPrincipal identity = new TestPrincipal();
            Assert.Null(identity.GetUserId());
            Assert.Null(identity.GetUserName());
        }

        [Fact]
        public void UserNameAndIdTest()
        {
            var p = CreateTestExternalIdentity();
            Assert.Equal("NameIdentifier", p.GetUserId());
            Assert.Equal("Name", p.GetUserName());
        }

        [Fact]
        public void IdentityExtensionsFindFirstValueNullIfUnknownTest()
        {
            var id = CreateTestExternalIdentity();
            Assert.Null(id.FindFirstValue("bogus"));
        }

        [Fact]
        public void IsSignedInWithDefaultAppAuthenticationType()
        {
            var id = CreateAppIdentity();
            Assert.True(id.IsSignedIn());
        }

        [Fact]
        public void IsSignedInFalseWithWrongAppAuthenticationType()
        {
            var id = CreateAppIdentity("bogus");
            Assert.False(id.IsSignedIn());
        }

        private static ClaimsPrincipal CreateAppIdentity(string authType = null)
        {
            authType = authType ?? IdentityOptions.ApplicationCookieAuthenticationType;
            return new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "NameIdentifier"),
                    new Claim(ClaimTypes.Name, "Name")
                },
                authType));
        }


        private static ClaimsPrincipal CreateTestExternalIdentity()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "NameIdentifier", null, ExternalAuthenticationScheme),
                    new Claim(ClaimTypes.Name, "Name")
                },
                ExternalAuthenticationScheme));
        }

        private class TestPrincipal : IPrincipal
        {
            public IIdentity Identity
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsInRole(string role)
            {
                throw new NotImplementedException();
            }
        }

        private class TestIdentity : IIdentity
        {
            public string AuthenticationType
            {
                get { throw new NotImplementedException(); }
            }

            public bool IsAuthenticated
            {
                get { throw new NotImplementedException(); }
            }

            public string Name
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}