// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

namespace Microsoft.AspNet.Identity.Security.Test
{
    public class IdentityExtensionsTest
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