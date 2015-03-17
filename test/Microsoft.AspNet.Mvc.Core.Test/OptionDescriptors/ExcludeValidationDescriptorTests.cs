// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class ExcludeValidationDescriptorTests
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotIExcludeTypeFromBodyValidation()
        {
            // Arrange
            var expected = "The type 'System.String' must derive from " +
                            "'Microsoft.AspNet.Mvc.ModelBinding.Validation.IExcludeTypeValidationFilter'.";
            var type = typeof(string);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new ExcludeValidationDescriptor(type), "type", expected);
        }

        [Fact]
        public void ConstructorSetsOptionType()
        {
            // Arrange
            var type = typeof(TestExcludeFilter);

            // Act
            var descriptor = new ExcludeValidationDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.OptionType);
            Assert.Null(descriptor.Instance);
        }

        [Fact]
        public void ConstructorSetsInstanceeAndOptionType()
        {
            // Arrange
            var instance = new TestExcludeFilter();

            // Act
            var descriptor = new ExcludeValidationDescriptor(instance);

            // Assert
            Assert.Same(instance, descriptor.Instance);
            Assert.Equal(instance.GetType(), descriptor.OptionType);
        }

        private class TestExcludeFilter : IExcludeTypeValidationFilter
        {
            public bool IsTypeExcluded([NotNull] Type propertyType)
            {
                throw new NotImplementedException();
            }
        }
    }
}