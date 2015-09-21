// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ExcludeTypeValidationFilterCollectionTest
    {
        [Fact]
        public void AddFilter_ByType()
        {
            // Arrange
            var type = typeof(BaseType);
            var collection = new ExcludeTypeValidationFilterCollection();

            // Act
            collection.Add(type);

            // Assert
            var filter = Assert.IsType<DefaultTypeBasedExcludeFilter>(Assert.Single(collection));
            Assert.Equal(type, filter.ExcludedType);
        }

        [Fact]
        public void AddFilter_ByTypeName()
        {
            // Arrange
            var type = typeof(BaseType);
            var collection = new ExcludeTypeValidationFilterCollection();

            // Act
            collection.Add(type.FullName);

            // Assert
            var filter = Assert.IsType<DefaultTypeNameBasedExcludeFilter>(Assert.Single(collection));
            Assert.Equal(type.FullName, filter.ExcludedTypeName);
        }

        private class BaseType
        {
        }
    }
}
