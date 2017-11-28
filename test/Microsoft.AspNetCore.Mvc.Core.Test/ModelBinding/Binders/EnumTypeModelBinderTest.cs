// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class EnumTypeModelBinderTest
    {
        [Theory]
        [InlineData(true, typeof(IntEnum?))]
        [InlineData(true, typeof(FlagsEnum?))]
        [InlineData(false, typeof(IntEnum?))]
        [InlineData(false, typeof(FlagsEnum?))]
        public async Task BindModel_SetsModel_ForEmptyValue_AndNullableEnumTypes(
            bool allowBindingUndefinedValueToEnumType,
            Type modelType)
        {
            // Arrange
            var binderInfo = GetBinderAndContext(
                modelType,
                allowBindingUndefinedValueToEnumType,
                valueProviderValue: "");
            var bindingContext = binderInfo.Item1;
            var binder = binderInfo.Item2;

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
        }

        [Theory]
        [InlineData(true, typeof(IntEnum))]
        [InlineData(true, typeof(FlagsEnum))]
        [InlineData(false, typeof(IntEnum))]
        [InlineData(false, typeof(FlagsEnum))]
        public async Task BindModel_AddsErrorToModelState_ForEmptyValue_AndNonNullableEnumTypes(
            bool allowBindingUndefinedValueToEnumType,
            Type modelType)
        {
            // Arrange
            var message = "The value '' is invalid.";
            var binderInfo = GetBinderAndContext(
                modelType,
                allowBindingUndefinedValueToEnumType,
                valueProviderValue: "");
            var bindingContext = binderInfo.Item1;
            var binder = binderInfo.Item2;

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Theory]
        [InlineData(true, "Value1")]
        [InlineData(true, "1")]
        [InlineData(false, "Value1")]
        [InlineData(false, "1")]
        public async Task BindModel_BindsEnumModels_ForValuesInArray(
            bool allowBindingUndefinedValueToEnumType,
            string enumValue)
        {
            // Arrange
            var modelType = typeof(IntEnum);
            var binderInfo = GetBinderAndContext(
                modelType,
                allowBindingUndefinedValueToEnumType,
                valueProviderValue: new object[] { enumValue });
            var bindingContext = binderInfo.Item1;
            var binder = binderInfo.Item2;

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
            Assert.Equal(IntEnum.Value1, boundModel);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("8, 1", true)]
        [InlineData("Value2, Value8", true)]
        [InlineData("value8,value4,value2,value1", true)]
        [InlineData("1", false)]
        [InlineData("8, 1", false)]
        [InlineData("Value2, Value8", false)]
        [InlineData("value8,value4,value2,value1", false)]
        public async Task BindModel_BindsTo_NonNullableFlagsEnumType(string flagsEnumValue, bool allowBindingUndefinedValueToEnumType)
        {
            // Arrange
            var modelType = typeof(FlagsEnum);
            var enumConverter = TypeDescriptor.GetConverter(modelType);
            var expected = enumConverter.ConvertFrom(flagsEnumValue).ToString();
            var binderInfo = GetBinderAndContext(
                modelType,
                allowBindingUndefinedValueToEnumType,
                valueProviderValue: new object[] { flagsEnumValue });
            var bindingContext = binderInfo.Item1;
            var binder = binderInfo.Item2;

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
            Assert.Equal(expected, boundModel.ToString());
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("8, 1", true)]
        [InlineData("Value2, Value8", true)]
        [InlineData("value8,value4,value2,value1", true)]
        [InlineData("1", false)]
        [InlineData("8, 1", false)]
        [InlineData("Value2, Value8", false)]
        [InlineData("value8,value4,value2,value1", false)]
        public async Task BindModel_BindsTo_NullableFlagsEnumType(string flagsEnumValue, bool allowBindingUndefinedValueToEnumType)
        {
            // Arrange
            var modelType = typeof(FlagsEnum?);
            var enumConverter = TypeDescriptor.GetConverter(GetUnderlyingType(modelType));
            var expected = enumConverter.ConvertFrom(flagsEnumValue).ToString();
            var binderInfo = GetBinderAndContext(
                modelType,
                allowBindingUndefinedValueToEnumType,
                valueProviderValue: new object[] { flagsEnumValue });
            var bindingContext = binderInfo.Item1;
            var binder = binderInfo.Item2;

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
            Assert.Equal(expected, boundModel.ToString());
        }

        [Theory]
        [InlineData(typeof(FlagsEnum), "Value10")]
        [InlineData(typeof(FlagsEnum), "Value1, Value10")]
        [InlineData(typeof(FlagsEnum), "value10, value1")]
        [InlineData(typeof(FlagsEnum?), "Value10")]
        [InlineData(typeof(FlagsEnum?), "Value1, Value10")]
        [InlineData(typeof(FlagsEnum?), "value10, value1")]
        public async Task BindModel_AddsErrorToModelState_ForInvalidEnumValues_IsNotValidMessage(Type modelType, string suppliedValue)
        {
            // Arrange
            var message = $"The value '{suppliedValue}' is not valid.";
            var binderInfo = GetBinderAndContext(
                modelType,
                allowBindingUndefinedValueToEnumType: true,
                valueProviderValue: new object[] { suppliedValue });
            var bindingContext = binderInfo.Item1;
            var binder = binderInfo.Item2;


            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Theory]
        [InlineData(typeof(IntEnum), "")]
        [InlineData(typeof(IntEnum), "3")]
        [InlineData(typeof(FlagsEnum), "19")]
        [InlineData(typeof(FlagsEnum), "0")]
        [InlineData(typeof(FlagsEnum), "1, 16")]
        // These two values look like big integers but are treated as two separate enum values that are
        // or'd together.
        [InlineData(typeof(FlagsEnum), "32,015")]
        [InlineData(typeof(FlagsEnum), "32,128")]
        [InlineData(typeof(IntEnum?), "3")]
        [InlineData(typeof(FlagsEnum?), "19")]
        [InlineData(typeof(FlagsEnum?), "0")]
        [InlineData(typeof(FlagsEnum?), "1, 16")]
        // These two values look like big integers but are treated as two separate enum values that are
        // or'd together.
        [InlineData(typeof(FlagsEnum?), "32,015")]
        [InlineData(typeof(FlagsEnum?), "32,128")]
        public async Task BindModel_AddsErrorToModelState_ForInvalidEnumValues(Type modelType, string suppliedValue)
        {
            // Arrange
            var message = $"The value '{suppliedValue}' is invalid.";
            var binderInfo = GetBinderAndContext(
                modelType,
                allowBindingUndefinedValueToEnumType: false,
                valueProviderValue: new object[] { suppliedValue });
            var bindingContext = binderInfo.Item1;
            var binder = binderInfo.Item2;

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
            Assert.Equal(message, error.ErrorMessage);
        }

        [Theory]
        [InlineData(typeof(IntEnum), "3", 3)]
        [InlineData(typeof(FlagsEnum), "19", 19)]
        [InlineData(typeof(FlagsEnum), "0", 0)]
        [InlineData(typeof(FlagsEnum), "1, 16", 17)]
        // These two values look like big integers but are treated as two separate enum values that are
        // or'd together.
        [InlineData(typeof(FlagsEnum), "32,015", 47)]
        [InlineData(typeof(FlagsEnum), "32,128", 160)]
        [InlineData(typeof(IntEnum?), "3", 3)]
        [InlineData(typeof(FlagsEnum?), "19", 19)]
        [InlineData(typeof(FlagsEnum?), "0", 0)]
        [InlineData(typeof(FlagsEnum?), "1, 16", 17)]
        // These two values look like big integers but are treated as two separate enum values that are
        // or'd together.
        [InlineData(typeof(FlagsEnum?), "32,015", 47)]
        [InlineData(typeof(FlagsEnum?), "32,128", 160)]
        public async Task BindModel_AllowsBindingUndefinedValues_ToEnumTypes(
            Type modelType,
            string suppliedValue,
            long expected)
        {
            // Arrange
            var binderProviderContext = new TestModelBinderProviderContext(modelType);
            var binderInfo = GetBinderAndContext(
                modelType,
                allowBindingUndefinedValueToEnumType: true,
                valueProviderValue: new object[] { suppliedValue });
            var bindingContext = binderInfo.Item1;
            var binder = binderInfo.Item2;

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.IsType(GetUnderlyingType(modelType), bindingContext.Result.Model);
            Assert.Equal(expected, Convert.ToInt64(bindingContext.Result.Model));
        }

        private static (DefaultModelBindingContext, IModelBinder) GetBinderAndContext(
            Type modelType,
            bool allowBindingUndefinedValueToEnumType,
            object valueProviderValue)
        {
            var binderProviderContext = new TestModelBinderProviderContext(modelType);
            var modelName = "theModelName";
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = binderProviderContext.Metadata,
                ModelName = modelName,
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleValueProvider()
                {
                    { modelName, valueProviderValue }
                }
            };
            var binderProvider = new EnumTypeModelBinderProvider(new MvcOptions
            {
                AllowBindingUndefinedValueToEnumType = allowBindingUndefinedValueToEnumType
            });
            var binder = binderProvider.GetBinder(binderProviderContext);
            return (bindingContext, binder);
        }

        private static Type GetUnderlyingType(Type modelType)
        {
            var underlyingType = Nullable.GetUnderlyingType(modelType);
            if (underlyingType != null)
            {
                return underlyingType;
            }
            return modelType;
        }

        [Flags]
        private enum FlagsEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value4 = 4,
            Value8 = 8,
        }

        private enum IntEnum
        {
            Value0 = 0,
            Value1 = 1,
            Value2 = 2,
            MaxValue = int.MaxValue
        }
    }
}
