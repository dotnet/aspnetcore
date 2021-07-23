// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Authorization.Test
{
    public class DenyAnonymousAuthorizationRequirementTests
    {
        private DenyAnonymousAuthorizationRequirement CreateRequirement()
        {
            return new DenyAnonymousAuthorizationRequirement();
        }

        [Fact]
        public void ToString_ShouldReturnFormatValue()
        {
            // Arrange
            var requirement = CreateRequirement();

            // Act
            var formattedValue = requirement.ToString();

            // Assert
            Assert.Equal("DenyAnonymousAuthorizationRequirement: Requires an authenticated user.", formattedValue);
        }
    }
}
