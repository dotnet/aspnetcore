// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class RouteOptionsTests
    {
        [Fact]
        public void ConstraintMap_SettingNullValue_Throws()
        {
            // Arrange
            var options = new RouteOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.ConstraintMap = null);
            Assert.Equal("The 'ConstraintMap' property of 'Microsoft.AspNet.Routing.RouteOptions' must not be null." +
                         "\r\nParameter name: value", ex.Message);
        }
    }
}
