// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class ByteArrayModelBinderTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task BindModelSetsModelToNullOnNullOrEmptyString(string value)
        {
            // Arrange
            var valueProvider = new SimpleValueProvider()
            {
                { "foo", value }
            };

            var bindingContext = GetBindingContext(valueProvider, typeof(byte[]));
            var binder = new ByteArrayModelBinder(NullLoggerFactory.Instance);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);

            var modelState = Assert.Single(bindingContext.ModelState);
            Assert.Equal("foo", modelState.Key);
            Assert.Equal(string.Empty, modelState.Value.RawValue);
        }

        [Fact]
        public async Task BindModel()
        {
            // Arrange
            var valueProvider = new SimpleValueProvider()
            {
                { "foo", "Fys1" }
            };

            var bindingContext = GetBindingContext(valueProvider, typeof(byte[]));
            var binder = new ByteArrayModelBinder(NullLoggerFactory.Instance);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var bytes = Assert.IsType<byte[]>(bindingContext.Result.Model);
            Assert.Equal(new byte[] { 23, 43, 53 }, bytes);
        }

        [Fact]
        public async Task BindModelAddsModelErrorsOnInvalidCharacters()
        {
            // Arrange
            var expected = "The value '\"Fys1\"' is not valid.";

            var valueProvider = new SimpleValueProvider()
            {
                { "foo", "\"Fys1\"" }
            };

            var bindingContext = GetBindingContext(valueProvider, typeof(byte[]));
            var binder = new ByteArrayModelBinder(NullLoggerFactory.Instance);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["foo"].Errors);
            Assert.Equal(expected, error.ErrorMessage);
        }

        [Fact]
        public async Task BindModel_ReturnsWithIsModelSetFalse_WhenValueNotFound()
        {
            // Arrange
            var valueProvider = new SimpleValueProvider()
            {
                { "someName", "" }
            };

            var bindingContext = GetBindingContext(valueProvider, typeof(byte[]));
            var binder = new ByteArrayModelBinder(NullLoggerFactory.Instance);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);

            Assert.Empty(bindingContext.ModelState); // No submitted data for "foo".
        }

        private static DefaultModelBindingContext GetBindingContext(IValueProvider valueProvider, Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "foo",
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
            };
            return bindingContext;
        }
    }
}
