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

        [Fact]
        public void IdentityExtensionsGetLoggedInUserIdInStringTypeTest()
        {
            // Arrange
            var id = CreateTestUserIdentityWithStringTypeUserId();

            // Act
            string loggedInUserId = id.GetLoggedInUserId<string>();

            // Assert
            Assert.IsType<string>(loggedInUserId);
        }

        [Fact]
        public void IdentityExtensionsGetLoggedInUserIdInLongTypeTest()
        {
            // Arrange
            var id = CreateTestUserIdentityWithLongTypeUserId();

            // Act
            long loggedInUserId = id.GetLoggedInUserId<long>();

            // Assert
            Assert.IsType<long>(loggedInUserId);
        }

        [Fact]
        public void IdentityExtensionsGetLoggedInUserNameTest()
        {
            // Arrange
            var id = CreateTestUserIdentityWithUserNameAndEmail();

            // Act
            string loggedInUserName = id.GetLoggedInUserName();

            // Assert
            Assert.Equal("BillGates", loggedInUserName);
        }

        [Fact]
        public void IdentityExtensionsGetLoggedInUserEmailTest()
        {
            // Arrange
            var id = CreateTestUserIdentityWithUserNameAndEmail();

            // Act
            string loggedInUserEmail = id.GetLoggedInUserEmail();

            // Assert
            Assert.Equal("bill@microsoft.com", loggedInUserEmail);
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

        private static ClaimsPrincipal CreateTestUserIdentityWithStringTypeUserId()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                },
                ExternalAuthenticationScheme));
        }

        private static ClaimsPrincipal CreateTestUserIdentityWithLongTypeUserId()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "123456789")
                },
                ExternalAuthenticationScheme));
        }

        private static ClaimsPrincipal CreateTestUserIdentityWithUserNameAndEmail()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, "BillGates"),
                    new Claim(ClaimTypes.Email,"bill@microsoft.com"), 
                },
                ExternalAuthenticationScheme));
        }
    }
}
