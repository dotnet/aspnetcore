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
        public const string ExternalAuthenticationType = "TestExternalAuth";

        [Fact]
        public void IdentityNullCheckTest()
        {
            IIdentity identity = null;
            Assert.Throws<ArgumentNullException>("identity", () => identity.GetUserId());
            Assert.Throws<ArgumentNullException>("identity", () => identity.GetUserName());
            ClaimsIdentity claimsIdentity = null;
            Assert.Throws<ArgumentNullException>("identity", () => claimsIdentity.FindFirstValue(null));
        }

        [Fact]
        public void IdentityNullIfNotClaimsIdentityTest()
        {
            IIdentity identity = new TestIdentity();
            Assert.Null(identity.GetUserId());
            Assert.Null(identity.GetUserName());
        }

        [Fact]
        public void UserNameAndIdTest()
        {
            var id = CreateTestExternalIdentity();
            Assert.Equal("NameIdentifier", id.GetUserId());
            Assert.Equal("Name", id.GetUserName());
        }

        [Fact]
        public void IdentityExtensionsFindFirstValueNullIfUnknownTest()
        {
            var id = CreateTestExternalIdentity();
            Assert.Null(id.FindFirstValue("bogus"));
        }

        private static ClaimsIdentity CreateTestExternalIdentity()
        {
            return new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "NameIdentifier", null, ExternalAuthenticationType),
                    new Claim(ClaimTypes.Name, "Name")
                },
                ExternalAuthenticationType);
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