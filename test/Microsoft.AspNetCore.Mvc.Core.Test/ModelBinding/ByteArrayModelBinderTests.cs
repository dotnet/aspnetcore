// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
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
            var binder = new ByteArrayModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(binderResult);
            Assert.False(binderResult.IsModelSet);
            Assert.Equal("foo", binderResult.Key);
            Assert.Null(binderResult.Model);

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
            var binder = new ByteArrayModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(binderResult);
            var bytes = Assert.IsType<byte[]>(binderResult.Model);
            Assert.Equal(new byte[] { 23, 43, 53 }, bytes);
        }

        [Fact]
        public async Task BindModelAddsModelErrorsOnInvalidCharacters()
        {
            // Arrange
            var expected = "The value '\"Fys1\"' is not valid for Byte[].";

            var valueProvider = new SimpleValueProvider()
            {
                { "foo", "\"Fys1\"" }
            };

            var bindingContext = GetBindingContext(valueProvider, typeof(byte[]));
            var binder = new ByteArrayModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(binderResult);
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
            var binder = new ByteArrayModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(binderResult);
            Assert.False(binderResult.IsModelSet);
            Assert.Equal("foo", binderResult.Key);
            Assert.Null(binderResult.Model);

            Assert.Empty(bindingContext.ModelState); // No submitted data for "foo".
        }

        [Fact]
        public async Task BindModel_ReturnsNull_ForOtherTypes()
        {
            // Arrange
            var bindingContext = GetBindingContext(new SimpleValueProvider(), typeof(int[]));
            var binder = new ByteArrayModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, binderResult);
        }

        private static ModelBindingContext GetBindingContext(IValueProvider valueProvider, Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "foo",
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
                OperationBindingContext  = new OperationBindingContext
                {
                    MetadataProvider = metadataProvider,
                }
            };
            return bindingContext;
        }
    }
}
