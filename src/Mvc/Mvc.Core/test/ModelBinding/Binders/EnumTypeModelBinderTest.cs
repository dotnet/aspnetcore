// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class EnumTypeModelBinderTest
{
    [Theory]
    [InlineData(typeof(IntEnum?))]
    [InlineData(typeof(FlagsEnum?))]
    public async Task BindModel_SetsModel_ForEmptyValue_AndNullableEnumTypes(Type modelType)
    {
        // Arrange
        var (bindingContext, binder) = GetBinderAndContext(modelType, valueProviderValue: "");

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    [Theory]
    [InlineData(typeof(IntEnum))]
    [InlineData(typeof(FlagsEnum))]
    public async Task BindModel_AddsErrorToModelState_ForEmptyValue_AndNonNullableEnumTypes(Type modelType)
    {
        // Arrange
        var message = "The value '' is invalid.";
        var (bindingContext, binder) = GetBinderAndContext(modelType, valueProviderValue: "");

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
    [InlineData("Value1")]
    [InlineData("1")]
    public async Task BindModel_BindsEnumModels_ForValuesInArray(string enumValue)
    {
        // Arrange
        var modelType = typeof(IntEnum);
        var (bindingContext, binder) = GetBinderAndContext(
            modelType,
            valueProviderValue: new object[] { enumValue });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
        Assert.Equal(IntEnum.Value1, boundModel);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("8, 1")]
    [InlineData("Value2, Value8")]
    [InlineData("value8,value4,value2,value1")]
    public async Task BindModel_BindsTo_NonNullableFlagsEnumType(string flagsEnumValue)
    {
        // Arrange
        var modelType = typeof(FlagsEnum);
        var enumConverter = TypeDescriptor.GetConverter(modelType);
        var expected = enumConverter.ConvertFrom(flagsEnumValue).ToString();
        var (bindingContext, binder) = GetBinderAndContext(
            modelType,
            valueProviderValue: new object[] { flagsEnumValue });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
        Assert.Equal(expected, boundModel.ToString());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("8, 1")]
    [InlineData("Value2, Value8")]
    [InlineData("value8,value4,value2,value1")]
    public async Task BindModel_BindsTo_NullableFlagsEnumType(string flagsEnumValue)
    {
        // Arrange
        var modelType = typeof(FlagsEnum?);
        var enumConverter = TypeDescriptor.GetConverter(GetUnderlyingType(modelType));
        var expected = enumConverter.ConvertFrom(flagsEnumValue).ToString();
        var (bindingContext, binder) = GetBinderAndContext(
            modelType,
            valueProviderValue: new object[] { flagsEnumValue });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
        Assert.Equal(expected, boundModel.ToString());
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
        var (bindingContext, binder) = GetBinderAndContext(
            modelType,
            valueProviderValue: new object[] { suppliedValue });

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
    [InlineData("8, 1")]
    [InlineData("Value2, Value8")]
    [InlineData("value8,value4,value2,value1")]
    public async Task BindModel_BindsTo_NonNullableFlagsEnumType_List(
        string flagsEnumValue
    )
    {
        // Arrange
        var modelType = typeof(FlagsEnum);
        var enumConverter = TypeDescriptor.GetConverter(modelType);
        var expected = enumConverter.ConvertFrom(flagsEnumValue).ToString();
        var (bindingContext, binder) = GetBinderAndContext(
            modelType,
            valueProviderValue: flagsEnumValue.Split(","));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
        Assert.Equal(expected, boundModel.ToString());
    }

    [Theory]
    [InlineData("8, 1")]
    [InlineData("Value2, Value8")]
    [InlineData("value8,value4,value2,value1")]
    public async Task BindModel_BindsTo_NullableFlagsEnumType_List(
        string flagsEnumValue
    )
    {
        // Arrange
        var modelType = typeof(FlagsEnum?);
        var enumConverter = TypeDescriptor.GetConverter(GetUnderlyingType(modelType));
        var expected = enumConverter.ConvertFrom(flagsEnumValue).ToString();
        var (bindingContext, binder) = GetBinderAndContext(
            modelType,
            valueProviderValue: flagsEnumValue.Split(","));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
        Assert.Equal(expected, boundModel.ToString());
    }

    [Theory]
    [InlineData(typeof(FlagsEnum), "1,16")]
    // These two values look like big integers but are treated as two separate enum values that are
    // or'd together.
    [InlineData(typeof(FlagsEnum), "32,015")]
    [InlineData(typeof(FlagsEnum), "32,128")]
    [InlineData(typeof(FlagsEnum?), "1,16")]
    // These two values look like big integers but are treated as two separate enum values that are
    // or'd together.
    [InlineData(typeof(FlagsEnum?), "32,015")]
    [InlineData(typeof(FlagsEnum?), "32,128")]
    public async Task BindModel_AddsErrorToModelState_ForInvalidEnumValues_List(
        Type modelType,
        string suppliedValue
    )
    {
        // Arrange
        var message = $"The value '{suppliedValue}' is invalid.";
        var (bindingContext, binder) = GetBinderAndContext(
            modelType,
            valueProviderValue: suppliedValue.Split(","));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
        Assert.False(bindingContext.ModelState.IsValid);
        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal(message, error.ErrorMessage);
    }

    private static (DefaultModelBindingContext, IModelBinder) GetBinderAndContext(
        Type modelType,
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

        var binderProvider = new EnumTypeModelBinderProvider(new MvcOptions());

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
