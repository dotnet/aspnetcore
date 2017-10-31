// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication
{
    public class ClaimActionTests
    {
        [Fact]
        public void CanMapSingleValueUserDataToClaim()
        {
            var userData = new JObject
            {
                ["name"] = "test"
            };

            var identity = new ClaimsIdentity();

            var action = new JsonKeyClaimAction("name", "name", "name");
            action.Run(userData, identity, "iss");

            Assert.Equal("name", identity.FindFirst("name").Type);
            Assert.Equal("test", identity.FindFirst("name").Value);
        }

        [Fact]
        public void CanMapArrayValueUserDataToClaims()
        {
            var userData = new JObject
            {
                ["role"] = new JArray { "role1", "role2" }
            };

            var identity = new ClaimsIdentity();

            var action = new JsonKeyClaimAction("role", "role", "role");
            action.Run(userData, identity, "iss");

            var roleClaims = identity.FindAll("role").ToList();
            Assert.Equal(2, roleClaims.Count);
            Assert.Equal("role", roleClaims[0].Type);
            Assert.Equal("role1", roleClaims[0].Value);
            Assert.Equal("role", roleClaims[1].Type);
            Assert.Equal("role2", roleClaims[1].Value);
        }
    }
}
