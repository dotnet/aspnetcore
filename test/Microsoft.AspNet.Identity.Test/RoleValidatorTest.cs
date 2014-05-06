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
    public class RoleValidatorTest
    {
        [Fact]
        public async Task ValidateThrowsWithNull()
        {
            // Setup
            var manager = new RoleManager<TestRole>(new ServiceCollection().BuildServiceProvider(), new NoopRoleStore());
            var validator = new RoleValidator<TestRole>();

            // Act
            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>("manager", async () => await validator.ValidateAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await validator.ValidateAsync(manager, null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ValidateFailsWithTooShortRoleName(string input)
        {
            // Setup
            var manager = new RoleManager<TestRole>(new ServiceCollection().BuildServiceProvider(), new NoopRoleStore());
            var validator = new RoleValidator<TestRole>();
            var user = new TestRole {Name = input};

            // Act
            var result = await validator.ValidateAsync(manager, user);

            // Assert
            IdentityResultAssert.IsFailure(result, "Name cannot be null or empty.");
        }
    }
}