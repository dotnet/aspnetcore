// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ExcludeTypeValidationFilterExtensionsTests
    {
        [Fact]
        public void InputFormatterDescriptors_AddsTypesAndTypeNames()
        {
            // Arrange
            var type1 = typeof(BaseType);
            var collection = new List<IExcludeTypeValidationFilter>();

            // Act
            collection.Add(type1);
            collection.Add(type1.FullName);

            // Assert
            Assert.Collection(collection,
                first =>
                {
                    var filter = Assert.IsType<DefaultTypeBasedExcludeFilter>(first);
                    Assert.Equal(type1, filter.ExcludedType);
                },
                second =>
                {
                    var filter = Assert.IsType<DefaultTypeNameBasedExcludeFilter>(second);
                    Assert.Equal(type1.FullName, filter.ExcludedTypeName);
                });
        }

        private class BaseType
        {
        }
    }
}
