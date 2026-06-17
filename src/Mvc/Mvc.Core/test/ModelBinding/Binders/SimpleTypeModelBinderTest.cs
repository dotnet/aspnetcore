// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class SimpleTypeModelBinderTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData("intermediate   whitespace")]
    [InlineData("\t untrimmed whitespace \t\r\n")]
    public async Task BindModelAsync_ReturnsProvidedString(string value)
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(string));
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", value }
            };

        var binder = new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Same(value, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t \r\n ")]
    public async Task BindModel_ReturnsProvidedWhitespaceString_WhenNotConvertEmptyStringToNull(string value)
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(string));
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", value }
            };

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForType(typeof(string))
            .DisplayDetails(d => d.ConvertEmptyStringToNull = false);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForType(typeof(string));

        var binder = new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Same(value, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    public static TheoryData<Type> ConvertibleTypeData
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
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_ReturnsFailure_IfTypeCanBeConverted_AndConversionFails(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "some-value" }
            };

        var binder = new SimpleTypeModelBinder(destinationType, NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_CreatesError_WhenTypeConversionIsNull(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", string.Empty }
            };
        var binder = new SimpleTypeModelBinder(destinationType, NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal("The value '' is invalid.", error.ErrorMessage, StringComparer.Ordinal);
        Assert.Null(error.Exception);
    }

    [Fact]
    public async Task BindModel_Error_FormatExceptionsTurnedIntoStringsInModelState()
    {
        // Arrange
        var message = "The value 'not an integer' is not valid.";
        var bindingContext = GetBindingContext(typeof(int));
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "not an integer" }
            };

        var binder = new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
        Assert.False(bindingContext.ModelState.IsValid);
        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal(message, error.ErrorMessage);
    }

    public static TheoryData<ModelMetadata> IntegerModelMetadataDataSet
    {
        get
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var method = typeof(MetadataClass).GetMethod(nameof(MetadataClass.IsLovely));
            var parameter = method.GetParameters()[0]; // IsLovely(int parameter)

            return new TheoryData<ModelMetadata>
                {
                    metadataProvider.GetMetadataForParameter(parameter),
                    metadataProvider.GetMetadataForProperty(typeof(MetadataClass), nameof(MetadataClass.Property)),
                    metadataProvider.GetMetadataForType(typeof(int)),
                };
        }
    }

    [Theory]
    [MemberData(nameof(IntegerModelMetadataDataSet))]
    public async Task BindModel_EmptyValueProviderResult_ReturnsFailedAndLogsSuccessfully(ModelMetadata metadata)
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(int));
        bindingContext.ModelMetadata = metadata;

        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var binder = new SimpleTypeModelBinder(typeof(int), loggerFactory);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Equal(ModelBindingResult.Failed(), bindingContext.Result);
        Assert.Empty(bindingContext.ModelState);
        Assert.Equal(3, sink.Writes.Count());
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

        var binder = new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Null(bindingContext.Result.Model);
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

        var binder = new SimpleTypeModelBinder(typeof(int?), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(12, bindingContext.Result.Model);
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

        var binder = new SimpleTypeModelBinder(typeof(double?), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(12.5, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    [Theory]
    [MemberData(nameof(IntegerModelMetadataDataSet))]
    public async Task BindModel_ValidValueProviderResult_ReturnsModelAndLogsSuccessfully(ModelMetadata metadata)
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(int));
        bindingContext.ModelMetadata = metadata;
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "42" }
            };

        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var binder = new SimpleTypeModelBinder(typeof(int), loggerFactory);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(42, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
        Assert.Equal(2, sink.Writes.Count());
    }

    public static TheoryData<Type> BiggerNumericTypes
    {
        get
        {
            // Data set does not include bool, byte, sbyte, or char because they do not need thousands separators.
            return new TheoryData<Type>
                {
                    typeof(decimal),
                    typeof(double),
                    typeof(float),
                    typeof(int),
                    typeof(long),
                    typeof(short),
                    typeof(uint),
                    typeof(ulong),
                    typeof(ushort),
                };
        }
    }

    [Theory]
    [MemberData(nameof(BiggerNumericTypes))]
    public async Task BindModel_ThousandsSeparators_LeadToErrors(Type type)
    {
        // Arrange
        var bindingContext = GetBindingContext(type);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", "32,000" }
            };

        var binder = new SimpleTypeModelBinder(type, NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);

        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal("theModelName", entry.Key);
        Assert.Equal("32,000", entry.Value.AttemptedValue);
        Assert.Equal(ModelValidationState.Invalid, entry.Value.ValidationState);

        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal("The value '32,000' is not valid.", error.ErrorMessage);
        Assert.Null(error.Exception);
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

        var binder = new SimpleTypeModelBinder(typeof(decimal), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(12.5M, bindingContext.Result.Model);
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

        var binder = new SimpleTypeModelBinder(typeof(decimal), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal("The value '12,5' is not valid.", error.ErrorMessage, StringComparer.Ordinal);
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

        var binder = new SimpleTypeModelBinder(typeof(IntEnum), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
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

        var binder = new SimpleTypeModelBinder(typeof(IntEnum), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
        Assert.Equal(IntEnum.Value1, boundModel);
    }

    public static TheoryData<string, int> EnumValues
    {
        get
        {
            return new TheoryData<string, int>
                {
                    { "0", 0 },
                    { "1", 1 },
                    { "13", 13 },
                    { "Value1", 1 },
                    { "Value1, Value2", 3 },
                    // These two values look like big integers but are treated as two separate enum values that are
                    // or'd together.
                    { "32,015", 47 },
                    { "32,128", 160 },
                };
        }
    }

    [Theory]
    [MemberData(nameof(EnumValues))]
    public async Task BindModel_BindsIntEnumModels(string flagsEnumValue, int expected)
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(IntEnum));
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", flagsEnumValue }
            };

        var binder = new SimpleTypeModelBinder(typeof(IntEnum), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<IntEnum>(bindingContext.Result.Model);
        Assert.Equal((IntEnum)expected, boundModel);
    }

    [Theory]
    [MemberData(nameof(EnumValues))]
    [InlineData("Value8, Value4", 12)]
    public async Task BindModel_BindsFlagsEnumModels(string flagsEnumValue, int expected)
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(FlagsEnum));
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", flagsEnumValue }
            };

        var binder = new SimpleTypeModelBinder(typeof(FlagsEnum), NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var boundModel = Assert.IsType<FlagsEnum>(bindingContext.Result.Model);
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
        Value8 = 8,
    }

    private enum IntEnum
    {
        Value0 = 0,
        Value1 = 1,
        Value2 = 2,
        MaxValue = int.MaxValue
    }

    private class MetadataClass
    {
        public int Property { get; set; }

        public bool IsLovely(int parameter)
        {
            return true;
        }
    }
}
