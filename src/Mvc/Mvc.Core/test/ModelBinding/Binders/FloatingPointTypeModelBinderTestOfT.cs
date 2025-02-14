// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public abstract class FloatingPointTypeModelBinderTest<TFloatingPoint> where TFloatingPoint : struct
{
    public static TheoryData<Type> ConvertibleTypeData
    {
        get
        {
            return new TheoryData<Type>
                {
                    typeof(TFloatingPoint),
                    typeof(TFloatingPoint?),
                };
        }
    }

    protected abstract TFloatingPoint Twelve { get; }

    protected abstract TFloatingPoint TwelvePointFive { get; }

    protected abstract TFloatingPoint ThirtyTwoThousand { get; }

    protected abstract TFloatingPoint ThirtyTwoThousandPointOne { get; }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_ReturnsFailure_IfAttemptedValueCannotBeParsed(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "some-value" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_CreatesError_IfAttemptedValueCannotBeParsed(Type destinationType)
    {
        // Arrange
        var message = "The value 'not a number' is not valid.";
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "not a number" },
            };
        var binder = GetBinder();

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
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_CreatesError_IfAttemptedValueCannotBeCompletelyParsed(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", "12_5" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal("The value '12_5' is not valid.", error.ErrorMessage, StringComparer.Ordinal);
        Assert.Null(error.Exception);
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_CreatesError_IfAttemptedValueContainsDisallowedWhitespace(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", " 12" }
            };
        var binder = GetBinder(NumberStyles.Float & ~NumberStyles.AllowLeadingWhite);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal("The value ' 12' is not valid.", error.ErrorMessage, StringComparer.Ordinal);
        Assert.Null(error.Exception);
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_CreatesError_IfAttemptedValueContainsDisallowedDecimal(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", "12.5" }
            };
        var binder = GetBinder(NumberStyles.Float & ~NumberStyles.AllowDecimalPoint);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal("The value '12.5' is not valid.", error.ErrorMessage, StringComparer.Ordinal);
        Assert.Null(error.Exception);
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_CreatesError_IfAttemptedValueContainsDisallowedThousandsSeparator(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", "32,000" }
            };
        var binder = GetBinder(NumberStyles.Float);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal("The value '32,000' is not valid.", error.ErrorMessage, StringComparer.Ordinal);
        Assert.Null(error.Exception);
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_ReturnsFailed_IfValueProviderEmpty(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Equal(ModelBindingResult.Failed(), bindingContext.Result);
        Assert.Empty(bindingContext.ModelState);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t \r\n ")]
    public async Task BindModel_CreatesError_IfTrimmedAttemptedValueIsEmpty_NonNullableDestination(string value)
    {
        // Arrange
        var message = $"The value '{value}' is invalid.";
        var bindingContext = GetBindingContext(typeof(TFloatingPoint));
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", value },
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal(message, error.ErrorMessage, StringComparer.Ordinal);
        Assert.Null(error.Exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t \r\n ")]
    public async Task BindModel_ReturnsNull_IfTrimmedAttemptedValueIsEmpty_NullableDestination(string value)
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(TFloatingPoint?));
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", value }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Null(bindingContext.Result.Model);
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal("theModelName", entry.Key);
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_ReturnsModel_IfAttemptedValueIsValid_Twelve(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "12" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(Twelve, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    [ReplaceCulture("en-GB", "en-GB")]
    public async Task BindModel_ReturnsModel_IfAttemptedValueIsValid_TwelvePointFive(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "12.5" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(TwelvePointFive, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_ReturnsModel_IfAttemptedValueIsValid_FrenchTwelvePointFive(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("fr-FR"))
            {
                { "theModelName", "12,5" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(TwelvePointFive, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_ReturnsModel_IfAttemptedValueIsValid_ThirtyTwoThousand(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", "32,000" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(ThirtyTwoThousand, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_ReturnsModel_IfAttemptedValueIsValid_ThirtyTwoThousandPointOne(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", "32,000.1" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(ThirtyTwoThousandPointOne, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypeData))]
    public async Task BindModel_ReturnsModel_IfAttemptedValueIsValid_FrenchThirtyTwoThousandPointOne(Type destinationType)
    {
        // Arrange
        var bindingContext = GetBindingContext(destinationType);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("fr-FR"))
            {
                { "theModelName", "32000,1" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(ThirtyTwoThousandPointOne, bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    protected abstract IModelBinder GetBinder(NumberStyles numberStyles);

    private IModelBinder GetBinder()
    {
        return GetBinder(FloatingPointTypeModelBinderProvider.SupportedStyles);
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
}
