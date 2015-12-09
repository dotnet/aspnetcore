// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class ClaimsIdentityExtensionsTest
    {
        public const string ExternalAuthenticationScheme = "TestExternalAuth";

        [Fact]
        public void IdentityNullCheckTest()
        {
            ClaimsPrincipal p = null;
            Assert.Throws<ArgumentNullException>("principal", () => p.FindFirstValue(null));
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
            authType = authType ?? new IdentityCookieOptions().ApplicationCookieAuthenticationScheme;
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
    }
}