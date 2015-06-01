// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class TypeMatchModelBinderTest
    {
        [Fact]
        public async Task BindModel_InvalidValueProviderResult_ReturnsNull()
        {
            // Arrange
            var bindingContext = GetBindingContext();
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "not an integer" }
            };

            var binder = new TypeMatchModelBinder();
            var modelState = bindingContext.ModelState;

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
            Assert.Empty(modelState.Keys);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task BindModel_ValidValueProviderResult_ReturnsNotNull()
        {
            // Arrange
            var bindingContext = GetBindingContext();
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", 42 }
            };

            var binder = new TypeMatchModelBinder();
            var modelState = bindingContext.ModelState;

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.Equal(42, result.Model);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("theModelName", key);
            Assert.Equal("42", modelState[key].Value.AttemptedValue);
            Assert.Equal(42, modelState[key].Value.RawValue);
        }

        [Fact]
        public async Task GetCompatibleValueProviderResult_ValueProviderResultRawValueIncorrect_ReturnsNull()
        {
            // Arrange
            var bindingContext = GetBindingContext();
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", "not an integer" }
            };

            // Act
            var vpResult = await TypeMatchModelBinder.GetCompatibleValueProviderResult(bindingContext);

            // Assert
            Assert.Null(vpResult); // Raw value is the wrong type
        }

        [Fact]
        public async Task GetCompatibleValueProviderResult_ValueProviderResultValid_ReturnsValueProviderResult()
        {
            // Arrange
            var bindingContext = GetBindingContext();
            bindingContext.ValueProvider = new SimpleHttpValueProvider
            {
                { "theModelName", 42 }
            };

            // Act
            var vpResult = await TypeMatchModelBinder.GetCompatibleValueProviderResult(bindingContext);

            // Assert
            Assert.NotNull(vpResult);
        }

        [Fact]
        public async Task GetCompatibleValueProviderResult_ValueProviderReturnsNull_ReturnsNull()
        {
            // Arrange
            var bindingContext = GetBindingContext();
            bindingContext.ValueProvider = new SimpleHttpValueProvider();

            // Act
            var vpResult = await TypeMatchModelBinder.GetCompatibleValueProviderResult(bindingContext);

            // Assert
            Assert.Null(vpResult); // No key matched
        }

        private static ModelBindingContext GetBindingContext()
        {
            return GetBindingContext(typeof(int));
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            return new ModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(modelType),
                ModelName = "theModelName"
            };
        }
    }
}
