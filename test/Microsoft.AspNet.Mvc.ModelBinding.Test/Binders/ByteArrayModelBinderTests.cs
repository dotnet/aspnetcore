// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
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
            var valueProvider = new SimpleHttpValueProvider()
            {
                { "foo", value }
            };

            var bindingContext = GetBindingContext(valueProvider, typeof(byte[]));
            var binder = new ByteArrayModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(binderResult);
        }

        [Fact]
        public async Task BindModel()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider()
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
            var expected = TestPlatformHelper.IsMono ?
                "Invalid length." :
                 "The supplied value is invalid for foo.";

            var valueProvider = new SimpleHttpValueProvider()
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
        public async Task BindModelReturnsFalseWhenValueNotFound()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider()
            {
                { "someName", "" }
            };

            var bindingContext = GetBindingContext(valueProvider, typeof(byte[]));
            var binder = new ByteArrayModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(binderResult);
        }

        [Fact]
        public async Task ByteArrayModelBinderReturnsFalseForOtherTypes()
        {
            // Arrange
            var bindingContext = GetBindingContext(null, typeof(int[]));
            var binder = new ByteArrayModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(binderResult);
        }

        private static ModelBindingContext GetBindingContext(IValueProvider valueProvider, Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "foo",
                ValueProvider = valueProvider,
                OperationBindingContext  = new OperationBindingContext
                {
                    MetadataProvider = metadataProvider
                }
            };
            return bindingContext;
        }
    }
}
#endif
