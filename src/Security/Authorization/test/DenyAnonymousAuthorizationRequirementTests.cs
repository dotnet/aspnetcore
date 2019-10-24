// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            Assert.Equal("DenyAnonymousAuthorizationRequirement:Requires an authenticated user.", formattedValue);
        }
    }
}
