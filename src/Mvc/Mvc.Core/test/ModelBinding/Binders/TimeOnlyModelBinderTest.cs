// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class TimeOnlyModelBinderTest
{
    [Fact]
    public async Task BindModel_ReturnsFailure_IfAttemptedValueCannotBeParsed()
    {
        // Arrange
        var bindingContext = GetBindingContext();
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

    [Fact]
    public async Task BindModel_CreatesError_IfAttemptedValueCannotBeParsed()
    {
        // Arrange
        var message = "The value 'not a time' is not valid.";
        var bindingContext = GetBindingContext();
        bindingContext.ValueProvider = new SimpleValueProvider
            {
                { "theModelName", "not a time" },
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

    [Fact]
    public async Task BindModel_CreatesError_IfAttemptedValueCannotBeCompletelyParsed()
    {
        // Arrange
        var bindingContext = GetBindingContext();
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("en-GB"))
            {
                { "theModelName", "11:05:08-not-a-time" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        var error = Assert.Single(bindingContext.ModelState["theModelName"].Errors);
        Assert.Equal("The value '11:05:08-not-a-time' is not valid.", error.ErrorMessage, StringComparer.Ordinal);
        Assert.Null(error.Exception);
    }

    [Fact]
    public async Task BindModel_ReturnsFailed_IfValueProviderEmpty()
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(TimeOnly));
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Equal(ModelBindingResult.Failed(), bindingContext.Result);
        Assert.Empty(bindingContext.ModelState);
    }

    [Fact]
    public async Task BindModel_NullableTimeOnly_ReturnsFailed_IfValueProviderEmpty()
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(TimeOnly?));
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
        var bindingContext = GetBindingContext();
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
        var bindingContext = GetBindingContext(typeof(TimeOnly?));
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
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(TimeOnly?))]
    public async Task BindModel_ReturnsModel_IfAttemptedValueIsValid(Type type)
    {
        // Arrange
        var expected = new TimeOnly(2, 30, 4, 0);
        var bindingContext = GetBindingContext(type);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("fr-FR"))
            {
                { "theModelName", "02:30:04.0000000" }
            };
        var binder = GetBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var model = Assert.IsType<TimeOnly>(bindingContext.Result.Model);
        Assert.Equal(expected, model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    [Fact]
    public async Task UsesSpecifiedStyleToParseModel()
    {
        // Arrange
        var bindingContext = GetBindingContext();
        var expected = TimeOnly.Parse("02:30:04.0000000", CultureInfo.InvariantCulture);
        bindingContext.ValueProvider = new SimpleValueProvider(new CultureInfo("fr-FR"))
            {
                { "theModelName", "   02:30:04.0000000" }
            };
        var binder = GetBinder(DateTimeStyles.AllowLeadingWhite);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var model = Assert.IsType<TimeOnly>(bindingContext.Result.Model);
        Assert.Equal(expected, model);
        Assert.True(bindingContext.ModelState.ContainsKey("theModelName"));
    }

    private IModelBinder GetBinder(DateTimeStyles? dateTimeStyles = null)
    {
        return new TimeOnlyModelBinder(dateTimeStyles ?? TimeOnlyModelBinderProvider.SupportedStyles, NullLoggerFactory.Instance);
    }

    private static DefaultModelBindingContext GetBindingContext(Type modelType = null)
    {
        modelType ??= typeof(TimeOnly);
        return new DefaultModelBindingContext
        {
            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(modelType),
            ModelName = "theModelName",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new SimpleValueProvider() // empty
        };
    }
}
