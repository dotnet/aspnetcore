// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class TypeConverterModelBinderTest
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Calendar))]
        [InlineData(typeof(TestClass))]
        public async Task BindModel_ReturnsFalse_IfTypeCannotBeConverted(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "some-value" }
            };

            var binder = new TypeConverterModelBinder();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(retVal);
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(short))]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(double))]
        [InlineData(typeof(DayOfWeek))]
        public async Task BindModel_ReturnsTrue_IfTypeCanBeConverted(Type destinationType)
        {
            if (TestPlatformHelper.IsMono &&
                destinationType == typeof(DateTimeOffset))
            {
                // DateTimeOffset doesn't have a TypeConverter in Mono
                return;
            }

            // Arrange
            var bindingContext = GetBindingContext(destinationType);
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "some-value" }
            };

            var binder = new TypeConverterModelBinder();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);
        }

        [Fact]
        public async Task BindModel_Error_FormatExceptionsTurnedIntoStringsInModelState()
        {
            // Arrange
            var message = "The parameter conversion from type 'System.String' to type 'System.Int32' failed." +
                " See the inner exception for more information.";
            var bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "not an integer" }
            };

            var binder = new TypeConverterModelBinder();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Null(bindingContext.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Fact]
        public async Task BindModel_NullValueProviderResult_ReturnsFalse()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(int));
            var binder = new TypeConverterModelBinder();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(retVal, "BindModel should have returned null.");
            Assert.Empty(bindingContext.ModelState);
        }

        [Fact]
        public async Task BindModel_ValidValueProviderResult_ConvertEmptyStringsToNull()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(string));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "" }
            };

            var binder = new TypeConverterModelBinder();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Null(bindingContext.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Fact]
        public async Task BindModel_ValidValueProviderResult_ReturnsModel()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "42" }
            };

            var binder = new TypeConverterModelBinder();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Equal(42, bindingContext.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            return new ModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, modelType),
                ModelName = "theModelName",
                ValueProvider = new SimpleHttpValueProvider() // empty
            };
        }

        private sealed class TestClass
        {
        }
    }
}
