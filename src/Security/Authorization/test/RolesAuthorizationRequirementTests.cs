// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Authorization.Test
{
    public class RolesAuthorizationRequirementTests
    {
        private RolesAuthorizationRequirement CreateRequirement(params string[] allowedRoles)
        {
            return new RolesAuthorizationRequirement(allowedRoles);
        }

        [Fact]
        public void ToString_ShouldReturnSplitByBarWhenHasTwoAllowedRoles()
        {
            // Arrange
            var requirement = CreateRequirement("Custom1", "Custom2");

            // Act
            var formattedValue = requirement.ToString();

            // Assert
            Assert.Equal("RolesAuthorizationRequirement:User.IsInRole must be true for one of the following roles: (Custom1|Custom2)", formattedValue);
        }
        
        [Fact]
        public void ToString_ShouldReturnUnSplitStringWhenOnlyOneAllowedRoles()
        {
            // Arrange
            var requirement = CreateRequirement("Custom1");

            // Act
            var formattedValue = requirement.ToString();

            // Assert
            Assert.Equal("RolesAuthorizationRequirement:User.IsInRole must be true for one of the following roles: (Custom1)",formattedValue);
        }
    }
}
