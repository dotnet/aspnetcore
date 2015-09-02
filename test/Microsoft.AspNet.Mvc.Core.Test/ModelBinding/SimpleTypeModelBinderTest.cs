// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class SimpleTypeModelBinderTest
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Calendar))]
        [InlineData(typeof(TestClass))]
        public async Task BindModel_ReturnsNoResult_IfTypeCannotBeConverted(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "some-value" }
            };

            var binder = new SimpleTypeModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        public static TheoryData<Type> ConvertableTypeData
        {
            get
            {
                var data = new TheoryData<Type>
                {
                    typeof(byte),
                    typeof(short),
                    typeof(int),
                    typeof(long),
                    typeof(Guid),
                    typeof(double),
                    typeof(DayOfWeek),
                };

                // DateTimeOffset doesn't have a TypeConverter in Mono.
                if (!TestPlatformHelper.IsMono)
                {
                    data.Add(typeof(DateTimeOffset));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ConvertableTypeData))]
        public async Task BindModel_ReturnsFailure_IfTypeCanBeConverted_AndConversionFails(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "some-value" }
            };

            var binder = new SimpleTypeModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.False(result.IsModelSet);
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(short))]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(double))]
        [InlineData(typeof(DayOfWeek))]
        public async Task BindModel_CreatesError_WhenTypeConversionIsNull(Type destinationType)
        {
            // Arrange
            var bindingContext = GetBindingContext(destinationType);
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", string.Empty }
            };
            var binder = new SimpleTypeModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);

            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(error.ErrorMessage, "The value '' is invalid.", StringComparer.Ordinal);
            Assert.Null(error.Exception);
        }

        [Fact]
        public async Task BindModel_Error_FormatExceptionsTurnedIntoStringsInModelState()
        {
            // Arrange
            var message = "The value 'not an integer' is not valid for theModelName.";
            var bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "not an integer" }
            };

            var binder = new SimpleTypeModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.Null(result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Fact]
        public async Task BindModel_NullValueProviderResult_ReturnsNull()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(int));
            var binder = new SimpleTypeModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
            Assert.Empty(bindingContext.ModelState);
        }

        [Fact]
        public async Task BindModel_ValidValueProviderResult_ConvertEmptyStringsToNull()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(string));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", string.Empty }
            };

            var binder = new SimpleTypeModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Fact]
        public async Task BindModel_ValidValueProviderResult_ReturnsModel()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "42" }
            };

            var binder = new SimpleTypeModelBinder();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(result.IsModelSet);
            Assert.Equal(42, result.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        private static ModelBindingContext GetBindingContext(Type modelType)
        {
            return new ModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(modelType),
                ModelName = "theModelName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleValueProvider() // empty
            };
        }

        private sealed class TestClass
        {
        }
    }
}
