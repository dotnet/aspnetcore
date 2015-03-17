// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class ModelValidatorProviderDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotModelValidatorProvider()
        {
            // Arrange
            var validatorProviderType = typeof(IModelValidatorProvider).FullName;
            var type = typeof(string);
            var expected = string.Format("The type '{0}' must derive from '{1}'.",
                                         type.FullName, validatorProviderType);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new ModelValidatorProviderDescriptor(type), "type", expected);
        }

        [Fact]
        public void ConstructorSetsModelValidatorProviderType()
        {
            // Arrange
            var type = typeof(TestModelValidatorProvider);

            // Act
            var descriptor = new ModelValidatorProviderDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.OptionType);
            Assert.Null(descriptor.Instance);
        }

        [Fact]
        public void ConstructorSetsInstanceAndType()
        {
            // Arrange
            var validatorProvider = new TestModelValidatorProvider();

            // Act
            var descriptor = new ModelValidatorProviderDescriptor(validatorProvider);

            // Assert
            Assert.Same(validatorProvider, descriptor.Instance);
            Assert.Equal(validatorProvider.GetType(), descriptor.OptionType);
        }

        private class TestModelValidatorProvider : IModelValidatorProvider
        {
            public void GetValidators(ModelValidatorProviderContext context)
            {
            }
        }
    }
}