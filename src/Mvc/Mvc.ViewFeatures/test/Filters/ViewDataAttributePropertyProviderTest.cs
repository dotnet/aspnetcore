// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    public class ViewDataAttributePropertyProviderTest
    {
        [Fact]
        public void GetViewDataProperties_ReturnsNull_IfTypeDoesNotHaveAnyViewDataProperties()
        {
            // Arrange
            var type = typeof(TestController_NoViewDataProperties);

            // Act
            var result = ViewDataAttributePropertyProvider.GetViewDataProperties(type);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetViewDataProperties_ReturnsViewDataProperties()
        {
            // Arrange
            var type = typeof(BaseController);

            // Act
            var result = ViewDataAttributePropertyProvider.GetViewDataProperties(type);

            // Assert
            Assert.Collection(
                result.OrderBy(p => p.Key),
                property =>
                {
                    Assert.Equal(nameof(BaseController.BaseProperty), property.PropertyInfo.Name);
                    Assert.Equal(nameof(BaseController.BaseProperty), property.Key);
                });
        }

        [Fact]
        public void GetViewDataProperties_ReturnsViewDataProperties_FromBaseTypes()
        {
            // Arrange
            var type = typeof(DerivedController);

            // Act
            var result = ViewDataAttributePropertyProvider.GetViewDataProperties(type);

            // Assert
            Assert.Collection(
                result.OrderBy(p => p.Key),
                property => Assert.Equal(nameof(BaseController.BaseProperty), property.PropertyInfo.Name),
                property => Assert.Equal(nameof(DerivedController.DerivedProperty), property.PropertyInfo.Name));
        }

        [Fact]
        public void GetViewDataProperties_UsesKeyFromViewDataAttribute()
        {
            // Arrange
            var type = typeof(PropertyWithKeyController);

            // Act
            var result = ViewDataAttributePropertyProvider.GetViewDataProperties(type);

            // Assert
            Assert.Collection(
                result.OrderBy(p => p.Key),
                property =>
                {
                    Assert.Equal(nameof(PropertyWithKeyController.Different), property.PropertyInfo.Name);
                    Assert.Equal("Test", property.Key);
                });
        }

        public class TestController_NoViewDataProperties
        {
            public DateTime? DateTime { get; set; }
        }

        public class BaseController
        {
            [ViewData]
            public string BaseProperty { get; }
        }

        public class DerivedController : BaseController
        {
            [ViewData]
            public string DerivedProperty { get; set; }
        }

        public class PropertyWithKeyController
        {
            [ViewData(Key = "Test")]
            public string Different { get; set; }
        }
    }
}
