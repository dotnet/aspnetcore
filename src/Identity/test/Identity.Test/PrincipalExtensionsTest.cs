// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class ClaimsIdentityExtensionsTest
    {
        public const string ExternalAuthenticationScheme = "TestExternalAuth";

        [Fact]
        public void IdentityExtensionsFindFirstValueNullIfUnknownTest()
        {
            var id = CreateTestExternalIdentity();
            Assert.Null(id.FindFirstValue("bogus"));
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