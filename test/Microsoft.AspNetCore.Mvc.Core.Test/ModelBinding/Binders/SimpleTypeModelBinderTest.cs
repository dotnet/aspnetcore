// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class SimpleTypeModelBinderTest
    {
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

            var binder = new SimpleTypeModelBinder(destinationType);

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
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
            var binder = new SimpleTypeModelBinder(destinationType);

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);

            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal("The value '' is invalid.", error.ErrorMessage, StringComparer.Ordinal);
            Assert.Null(error.Exception);
        }

        [Fact]
        public async Task BindModel_Error_FormatExceptionsTurnedIntoStringsInModelState()
        {
            // Arrange
            var message = "The value 'not an integer' is not valid for Int32.";
            var bindingContext = GetBindingContext(typeof(int));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "not an integer" }
            };

            var binder = new SimpleTypeModelBinder(typeof(int));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Fact]
        public async Task BindModel_EmptyValueProviderResult_ReturnsFailed()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(int));
            var binder = new SimpleTypeModelBinder(typeof(int));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.Failed(), result);
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

            var binder = new SimpleTypeModelBinder(typeof(string));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.Null(result.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t \r\n ")]
        public async Task BindModel_ReturnsNull_IfTrimmedValueIsEmptyString(object value)
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(string));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", value }
            };

            var binder = new SimpleTypeModelBinder(typeof(string));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.Null(result.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Fact]
        public async Task BindModel_NullableIntegerValueProviderResult_ReturnsModel()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(int?));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "12" }
            };

            var binder = new SimpleTypeModelBinder(typeof(int?));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.True(result.IsModelSet);
            Assert.Equal(12, result.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Fact]
        public async Task BindModel_NullableDoubleValueProviderResult_ReturnsModel()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(double?));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "12.5" }
            };

            var binder = new SimpleTypeModelBinder(typeof(double?));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.True(result.IsModelSet);
            Assert.Equal(12.5, result.Model);
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

            var binder = new SimpleTypeModelBinder(typeof(int));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.True(result.IsModelSet);
            Assert.Equal(42, result.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Fact]
        public async Task BindModel_ValidValueProviderResultWithProvidedCulture_ReturnsModel()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(decimal));
            bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("fr-FR"))
            {
                { "theModelName", "12,5" }
            };

            var binder = new SimpleTypeModelBinder(typeof(decimal));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.True(result.IsModelSet);
            Assert.Equal(12.5M, result.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        }

        [Fact]
        public async Task BindModel_CreatesErrorForFormatException_ValueProviderResultWithInvalidCulture()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(decimal));
            bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", "12,5" }
            };

            var binder = new SimpleTypeModelBinder(typeof(decimal));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.False(result.IsModelSet);
            Assert.Null(result.Model);

            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal("The value '12,5' is not valid for Decimal.", error.ErrorMessage, StringComparer.Ordinal);
            Assert.Null(error.Exception);
        }

        [Fact]
        public async Task BindModel_BindsEnumModels_IfArrayElementIsStringKey()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(IntEnum));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", new object[] { "Value1" } }
            };

            var binder = new SimpleTypeModelBinder(typeof(IntEnum));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.True(result.IsModelSet);
            var boundModel = Assert.IsType<IntEnum>(result.Model);
            Assert.Equal(IntEnum.Value1, boundModel);
        }

        [Fact]
        public async Task BindModel_BindsEnumModels_IfArrayElementIsStringValue()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(IntEnum));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", new object[] { "1" } }
            };

            var binder = new SimpleTypeModelBinder(typeof(IntEnum));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.True(result.IsModelSet);
            var boundModel = Assert.IsType<IntEnum>(result.Model);
            Assert.Equal(IntEnum.Value1, boundModel);
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        [InlineData("13", 13)]
        [InlineData("Value1", 1)]
        [InlineData("Value8, Value4", 12)]
        public async Task BindModel_BindsFlagsEnumModels(string flagsEnumValue, int expected)
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(FlagsEnum));
            bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", flagsEnumValue }
            };

            var binder = new SimpleTypeModelBinder(typeof(FlagsEnum));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.True(result.IsModelSet);
            var boundModel = Assert.IsType<FlagsEnum>(result.Model);
            Assert.Equal((FlagsEnum)expected, boundModel);
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType)
        {
            return new DefaultModelBindingContext
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

        [Flags]
        private enum FlagsEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value4 = 4,
            Value8 = 8
        }

        private enum IntEnum
        {
            Value0 = 0,
            Value1 = 1,
            MaxValue = int.MaxValue
        }
    }
}
