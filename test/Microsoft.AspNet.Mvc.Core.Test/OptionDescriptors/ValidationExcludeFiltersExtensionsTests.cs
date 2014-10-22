// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ValidationExcludeFiltersExtensionsTests
    {
        [Fact]
        public void InputFormatterDescriptors_AddsTypesAndTypeNames()
        {
            // Arrange
            var type1 = typeof(BaseType);
            var collection = new List<ExcludeValidationDescriptor>();

            // Act
            collection.Add(type1);
            collection.Add(type1.FullName);

            // Assert
            Assert.Equal(2, collection.Count);
            Assert.Equal(typeof(DefaultTypeBasedExcludeFilter), collection[0].OptionType);
            Assert.Equal(typeof(DefaultTypeNameBasedExcludeFilter), collection[1].OptionType);
        }

        private class BaseType
        {
        }
    }
}
