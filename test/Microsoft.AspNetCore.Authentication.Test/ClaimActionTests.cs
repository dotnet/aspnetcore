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

        [Fact]
        public void MapAllSucceeds()
        {
            var userData = new JObject
            {
                ["name0"] = "value0",
                ["name1"] = "value1",
            };

            var identity = new ClaimsIdentity();
            var action = new MapAllClaimsAction();
            action.Run(userData, identity, "iss");

            Assert.Equal("name0", identity.FindFirst("name0").Type);
            Assert.Equal("value0", identity.FindFirst("name0").Value);
            Assert.Equal("name1", identity.FindFirst("name1").Type);
            Assert.Equal("value1", identity.FindFirst("name1").Value);
        }

        [Fact]
        public void MapAllAllowesDulicateKeysWithUniqueValues()
        {
            var userData = new JObject
            {
                ["name0"] = "value0",
                ["name1"] = "value1",
            };

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("name0", "value2"));
            identity.AddClaim(new Claim("name1", "value3"));
            var action = new MapAllClaimsAction();
            action.Run(userData, identity, "iss");

            Assert.Equal(2, identity.FindAll("name0").Count());
            Assert.Equal(2, identity.FindAll("name1").Count());
        }

        [Fact]
        public void MapAllSkipsDuplicateValues()
        {
            var userData = new JObject
            {
                ["name0"] = "value0",
                ["name1"] = "value1",
            };

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("name0", "value0"));
            identity.AddClaim(new Claim("name1", "value1"));
            var action = new MapAllClaimsAction();
            action.Run(userData, identity, "iss");

            Assert.Single(identity.FindAll("name0"));
            Assert.Single(identity.FindAll("name1"));
        }
    }
}
