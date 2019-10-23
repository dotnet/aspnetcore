// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Authorization.Test
{
    public class ClaimsAuthorizationRequirementTests
    {
        public ClaimsAuthorizationRequirement CreateRequirement(string claimType, params string[] allowedValues)
        {
            return new ClaimsAuthorizationRequirement(claimType, allowedValues);
        }

        [Fact]
        public void ToString_ShouldReturnAndDescriptionWhenAllowedValuesNotNull()
        {
            // Arrange
            var requirement = CreateRequirement("Custom", "CustomValue1", "CustomValue2");

            // Act
            var formattedValue = requirement.ToString();

            // Assert
            Assert.Equal("ClaimsAuthorizationRequirement:Claim.Type=Custom and Claim.Value is one of the following values: (CustomValue1|CustomValue2)", formattedValue);
        }
        
        [Fact]
        public void ToString_ShouldReturnWithoutAllowedDescriptionWhenAllowedValuesIsNull()
        {
            // Arrange
            var requirement = CreateRequirement("Custom", (string[])null);

            // Act
            var formattedValue = requirement.ToString();

            // Assert
            Assert.Equal("ClaimsAuthorizationRequirement:Claim.Type=Custom", formattedValue);
        }
        
        [Fact]
        public void ToString_ShouldReturnWithoutAllowedDescriptionWhenAllowedValuesIsEmpty()
        {
            // Arrange
            var requirement = CreateRequirement("Custom", Array.Empty<string>());

            // Act
            var formattedValue = requirement.ToString();

            // Assert
            Assert.Equal("ClaimsAuthorizationRequirement:Claim.Type=Custom", formattedValue);
        }
    }
}
