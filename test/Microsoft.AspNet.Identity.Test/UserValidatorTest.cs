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
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class UserValidatorTest
    {
        [Fact]
        public async Task ValidateThrowsWithNull()
        {
            // Setup
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            var validator = new UserValidator<TestUser>();

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>("manager", () => validator.ValidateAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", () => validator.ValidateAsync(manager, null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ValidateFailsWithTooShortUserNames(string input)
        {
            // Setup
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            var validator = new UserValidator<TestUser>();
            var user = new TestUser {UserName = input};

            // Act
            var result = await validator.ValidateAsync(manager, user);

            // Assert
            IdentityResultAssert.IsFailure(result, "UserName cannot be null or empty.");
        }

        [Theory]
        [InlineData("test_email@foo.com", true)]
        [InlineData("hao", true)]
        [InlineData("test123", true)]
        [InlineData("!noway", false)]
        [InlineData("foo@boz#.com", false)]
        public async Task DefaultAlphaNumericOnlyUserNameValidation(string userName, bool expectSuccess)
        {
            // Setup
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            var validator = new UserValidator<TestUser>();
            var user = new TestUser {UserName = userName};

            // Act
            var result = await validator.ValidateAsync(manager, user);

            // Assert
            if (expectSuccess)
            {
                IdentityResultAssert.IsSuccess(result);
            }
            else
            {
                IdentityResultAssert.IsFailure(result);
            }
        }

        [Theory]
        [InlineData("test_email@foo.com", true)]
        [InlineData("hao", true)]
        [InlineData("test123", true)]
        [InlineData("!noway", true)]
        [InlineData("foo@boz#.com", true)]
        public async Task CanAllowNonAlphaNumericUserName(string userName, bool expectSuccess)
        {
            // Setup
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            manager.Options.User.AllowOnlyAlphanumericNames = false;
            var validator = new UserValidator<TestUser>();
            var user = new TestUser {UserName = userName};

            // Act
            var result = await validator.ValidateAsync(manager, user);

            // Assert
            if (expectSuccess)
            {
                IdentityResultAssert.IsSuccess(result);
            }
            else
            {
                IdentityResultAssert.IsFailure(result);
            }
        }
    }
}