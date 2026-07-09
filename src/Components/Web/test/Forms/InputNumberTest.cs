// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputNumberTest
{
    private readonly TestRenderer _testRenderer;

    public InputNumberTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _testRenderer = new TestRenderer(services.BuildServiceProvider());
    }

    [Fact]
    public async Task ValidationErrorUsesDisplayAttributeName()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "DisplayName", "Some number" }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.SomeNumber);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputComponent.SetCurrentValueAsStringAsync("notANumber");

        // Assert
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The Some number field must be a number.", validationMessages);
    }

    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };

        // Act
        var inputNumberComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert
        Assert.NotNull(inputNumberComponent.Element);
    }

    [Fact]
    public async Task UserDefinedTypeAttributeOverridesDefault()
    {
        // Arrange
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "type", "range" }  // User-defined 'type' attribute to override default
            }
        };

        // Act
        var componentId = await RenderAndGetComponentIdAsync(hostComponent);

        // Retrieve the render tree frames and extract attributes using helper methods
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttributeFrame = frames.Array.Single(frame =>
            frame.FrameType == RenderTreeFrameType.Attribute &&
            frame.AttributeName == "type");

        // Assert
        Assert.Equal("range", typeAttributeFrame.AttributeValue);
    }

    [Fact]
    public async Task RendersIdAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };

        var componentId = await RenderAndGetComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.NotNull(idAttribute.AttributeName);
        Assert.Equal("model_SomeNumber", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task ExplicitIdOverridesGenerated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
            AdditionalAttributes = new Dictionary<string, object> { { "id", "custom-number-id" } }
        };

        var componentId = await RenderAndGetComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("custom-number-id", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task DefaultTypeAttributeIsNumber()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };

        var componentId = await RenderAndGetComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttribute = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type");
        Assert.NotNull(typeAttribute.AttributeName);
        Assert.Equal("number", typeAttribute.AttributeValue);
    }

    [Fact]
    public async Task DefaultStepAttributeIsAny()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };

        var componentId = await RenderAndGetComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var stepAttribute = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "step");
        Assert.NotNull(stepAttribute.AttributeName);
        Assert.Equal("any", stepAttribute.AttributeValue);
    }

    [Fact]
    public async Task ValidNumericInputUpdatesModel()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNumber,
            ValueChanged = value => model.SomeNumber = value,
            ValueExpression = () => model.SomeNumber,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.SomeNumber);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("42");

        Assert.Equal(42, model.SomeNumber);
        Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
    }

    [Fact]
    public async Task ValidationErrorUsesFieldNameWhenDisplayNameMissing()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.SomeNumber);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("bad-value");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        var message = Assert.Single(validationMessages);
        Assert.Equal("The SomeNumber field must be a number.", message);
    }

    [Fact]
    public async Task NullableInputAcceptsEmptyString()
    {
        var model = new NullableTestModel();
        var rootComponent = new TestInputHostComponent<int?, NullableTestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNullableNumber,
            ValueChanged = value => model.SomeNullableNumber = value,
            ValueExpression = () => model.SomeNullableNumber,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.SomeNullableNumber);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync(string.Empty);

        Assert.Null(model.SomeNullableNumber);
        Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
    }

    [Fact]
    public void FormatValueAsStringFormatsIntegralTypes()
    {
        Assert.Equal(42.ToString(CultureInfo.InvariantCulture), FormatValue(42));
        Assert.Equal(long.MinValue.ToString(CultureInfo.InvariantCulture), FormatValue(long.MinValue));
        Assert.Equal(((short)-7).ToString(CultureInfo.InvariantCulture), FormatValue((short)-7));
    }

    [Fact]
    public void FormatValueAsStringFormatsFloatingPointTypesUsingInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            float floatValue = 3.142f;
            double doubleValue = 3.142;
            decimal decimalValue = 3.142m;

            Assert.Equal(BindConverter.FormatValue(floatValue, CultureInfo.InvariantCulture), FormatValue(floatValue));
            Assert.Equal(BindConverter.FormatValue(doubleValue, CultureInfo.InvariantCulture), FormatValue(doubleValue));
            Assert.Equal(BindConverter.FormatValue(decimalValue, CultureInfo.InvariantCulture), FormatValue(decimalValue));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Fact]
    public void FormatValueAsStringReturnsNullForNullValues()
    {
        Assert.Null(FormatValue<int?>(null));
    }

    [Fact]
    public void TryParseValueFromStringUsesInvariantCultureRegardlessOfCurrentCulture()
    {
        var component = new StandaloneInputNumber<double>();
        component.SetFieldName("InvariantNumber");

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var success = component.TryParseValue("3.14", out var result, out var validationErrorMessage);

            Assert.True(success);
            Assert.Equal(3.14, result, 3);
            Assert.Null(validationErrorMessage);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Fact]
    public void TryParseValueFromStringRejectsNullForNonNullable()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("NonNullable");

        var success = component.TryParseValue(null, out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.Equal("The NonNullable field must be a number.", validationErrorMessage);
    }

    [Fact]
    public void TryParseValueFromStringParsesBoundaryValues()
    {
        var component = new StandaloneInputNumber<long>();
        component.SetFieldName("BoundaryField");

        var success = component.TryParseValue(long.MaxValue.ToString(CultureInfo.InvariantCulture), out var result, out var validationErrorMessage);

        Assert.True(success);
        Assert.Equal(long.MaxValue, result);
        Assert.Null(validationErrorMessage);
    }

    [Fact]
    public void ParsingErrorMessageIsCustomizable()
    {
        var component = new StandaloneInputNumber<int>
        {
            ParsingErrorMessage = "Custom parsing failure for {0}."
        };
        component.SetFieldName("CustomNumber");

        var success = component.TryParseValue("oops", out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.Equal("Custom parsing failure for CustomNumber.", validationErrorMessage);
    }

    [Fact]
    public async Task UnsupportedNumericTypeThrowsDuringRendering()
    {
        var model = new GuidTestModel();
        var hostComponent = new TestInputHostComponent<Guid, GuidTestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeGuid,
        };

        var exception = await Assert.ThrowsAsync<TypeInitializationException>(async () =>
        {
            await InputRenderer.RenderAndGetComponent(hostComponent);
        });

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("Guid", exception.InnerException!.Message);
    }

    [Theory]
    [InlineData(-2147483648, "int.MinValue")]
    [InlineData(2147483647, "int.MaxValue")]
    [InlineData(0, "int.Zero")]
    public void ParsesIntBoundaryValues(int value, string scenario)
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName(scenario);

        var success = component.TryParseValue(value.ToString(CultureInfo.InvariantCulture), out var result, out _);

        Assert.True(success);
        Assert.Equal(value, result);
    }

    [Theory]
    [InlineData(-9223372036854775808L, "long.MinValue")]
    [InlineData(9223372036854775807L, "long.MaxValue")]
    [InlineData(0L, "long.Zero")]
    public void ParsesLongBoundaryValues(long value, string scenario)
    {
        var component = new StandaloneInputNumber<long>();
        component.SetFieldName(scenario);

        var success = component.TryParseValue(value.ToString(CultureInfo.InvariantCulture), out var result, out _);

        Assert.True(success);
        Assert.Equal(value, result);
    }

    [Theory]
    [InlineData(-2147483648, "int.MinValue")]
    [InlineData(2147483647, "int.MaxValue")]
    [InlineData(0, "int.Zero")]
    public void FormatsIntBoundaryValues(int value, string scenario)
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName(scenario);
        var formatted = component.FormatValue(value);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Theory]
    [InlineData(-9223372036854775808L, "long.MinValue")]
    [InlineData(9223372036854775807L, "long.MaxValue")]
    public void FormatsLongBoundaryValues(long value, string scenario)
    {
        var component = new StandaloneInputNumber<long>();
        component.SetFieldName(scenario);
        var formatted = component.FormatValue(value);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Theory]
    [MemberData(nameof(DecimalBoundaryValues))]
    public void FormatsDecimalBoundaryValues(decimal value, string scenario)
    {
        var component = new StandaloneInputNumber<decimal>();
        component.SetFieldName(scenario);
        var formatted = component.FormatValue(value);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Fact]
    public void ParsesFloatNegativeValue()
    {
        var component = new StandaloneInputNumber<float>();
        component.SetFieldName("FloatNegative");

        var success = component.TryParseValue("-123.456", out var result, out _);

        Assert.True(success);
        Assert.Equal(-123.456f, result, 2);
    }

    [Fact]
    public void ParsesFloatLargePositiveValue()
    {
        var component = new StandaloneInputNumber<float>();
        component.SetFieldName("FloatLarge");

        var success = component.TryParseValue("999.999", out var result, out _);

        Assert.True(success);
        Assert.Equal(999.999f, result, 2);
    }

    [Fact]
    public void ParsesDoubleLargeNegativeValue()
    {
        var component = new StandaloneInputNumber<double>();
        component.SetFieldName("DoubleLargeNeg");

        var success = component.TryParseValue("-1700000000000000000000000", out var result, out _);

        Assert.True(success);
        Assert.Equal(-1700000000000000000000000d, result);
    }

    [Fact]
    public void ParsesDoubleLargePositiveValue()
    {
        var component = new StandaloneInputNumber<double>();
        component.SetFieldName("DoubleLargePos");

        var success = component.TryParseValue("1700000000000000000000000", out var result, out _);

        Assert.True(success);
        Assert.Equal(1700000000000000000000000d, result);
    }

    [Theory]
    [MemberData(nameof(DecimalBoundaryValues))]
    public void ParsesDecimalBoundaryValues(decimal value, string scenario)
    {
        var component = new StandaloneInputNumber<decimal>();
        component.SetFieldName(scenario);

        var success = component.TryParseValue(value.ToString(CultureInfo.InvariantCulture), out var result, out _);

        Assert.True(success);
        Assert.Equal(value, result);
    }

    [Fact]
    public void FormatsFloatNegativeValue()
    {
        var value = -123.456f;
        var formatted = FormatValue(value);
        Assert.NotNull(formatted);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Fact]
    public void FormatsFloatLargePositiveValue()
    {
        var value = 999.999f;
        var formatted = FormatValue(value);
        Assert.NotNull(formatted);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Fact]
    public void FormatsDoubleLargeNegativeValue()
    {
        var value = -1700000000000000000000000d;
        var formatted = FormatValue(value);
        Assert.NotNull(formatted);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Fact]
    public void FormatsDoubleLargePositiveValue()
    {
        var value = 1700000000000000000000000d;
        var formatted = FormatValue(value);
        Assert.NotNull(formatted);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Theory]
    [InlineData("3.14")]
    [InlineData("")]
    public void ParsesNullableFloatCorrectly(string input)
    {
        var component = new StandaloneInputNumber<float?>();
        component.SetFieldName("NullableFloat");

        var success = component.TryParseValue(input, out var result, out _);

        if (input == string.Empty)
        {
            Assert.True(success);
            Assert.Null(result);
        }
        else
        {
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Equal(3.14f, result.Value, 2);
        }
    }

    [Theory]
    [InlineData("2.718")]
    [InlineData("")]
    public void ParsesNullableDoubleCorrectly(string input)
    {
        var component = new StandaloneInputNumber<double?>();
        component.SetFieldName("NullableDouble");

        var success = component.TryParseValue(input, out var result, out _);

        if (input == string.Empty)
        {
            Assert.True(success);
            Assert.Null(result);
        }
        else
        {
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Equal(2.718, result.Value, 3);
        }
    }

    [Theory]
    [InlineData("99.99")]
    [InlineData("")]
    public void ParsesNullableDecimalCorrectly(string input)
    {
        var component = new StandaloneInputNumber<decimal?>();
        component.SetFieldName("NullableDecimal");

        var success = component.TryParseValue(input, out var result, out _);

        if (input == string.Empty)
        {
            Assert.True(success);
            Assert.Null(result);
        }
        else
        {
            Assert.True(success);
            Assert.Equal(99.99m, result);
        }
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("")]
    public void ParsesNullableLongCorrectly(string input)
    {
        var component = new StandaloneInputNumber<long?>();
        component.SetFieldName("NullableLong");

        var success = component.TryParseValue(input, out var result, out _);

        if (input == string.Empty)
        {
            Assert.True(success);
            Assert.Null(result);
        }
        else
        {
            Assert.True(success);
            Assert.Equal(123456789L, result);
        }
    }

    [Theory]
    [InlineData("   ", false, 0, "whitespace-only")]
    [InlineData("  42", true, 42, "leading-whitespace")]
    [InlineData("42  ", true, 42, "trailing-whitespace")]
    [InlineData(" 42 ", true, 42, "both-whitespaces")]
    public void HandlesWhitespaceInNumericInput(string input, bool shouldSucceed, int expectedValue, string scenario)
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName($"Whitespace_{scenario}");

        var success = component.TryParseValue(input, out var result, out var errorMessage);

        Assert.Equal(shouldSucceed, success);
        if (shouldSucceed)
        {
            Assert.Equal(expectedValue, result);
        }
        else
        {
            Assert.NotNull(errorMessage);
        }
    }

    [Theory]
    [InlineData("1.5e10", false, "scientific-notation")]
    [InlineData("0x2A", false, "hex-notation")]
    [InlineData("$99.99", false, "currency-symbol")]
    [InlineData("--42", false, "double-negative")]
    [InlineData("3.14.159", false, "multiple-decimal-points")]
    [InlineData("", false, "empty-string-non-nullable")]
    public void RejectsInvalidInputPatterns(string input, bool shouldSucceed, string pattern)
    {
        if (pattern == "empty-string-non-nullable")
        {
            var component = new StandaloneInputNumber<int>();
            component.SetFieldName($"Invalid_{pattern}");
            var success = component.TryParseValue(input, out int _, out string errorMessage);
            Assert.Equal(shouldSucceed, success);
            Assert.NotNull(errorMessage);
        }
        else
        {
            var component = new StandaloneInputNumber<double>();
            component.SetFieldName($"Invalid_{pattern}");
            var success = component.TryParseValue(input, out double _, out string errorMessage);
            Assert.Equal(shouldSucceed, success);
            Assert.NotNull(errorMessage);
        }
    }

    [Theory]
    [InlineData("+42", 42, "plus-sign")]
    [InlineData("-123", -123, "negative-sign")]
    public void AcceptsValidSignPatterns(string input, int expectedValue, string pattern)
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName($"Sign_{pattern}");

        var success = component.TryParseValue(input, out var result, out _);

        Assert.True(success);
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task ValueChangedCallbackNotFiredOnInvalidInput()
    {
        var model = new TestModel();
        var valueChangedFired = false;
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNumber,
            ValueChanged = value =>
            {
                valueChangedFired = true;
                model.SomeNumber = value;
            },
            ValueExpression = () => model.SomeNumber,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalid");

        Assert.False(valueChangedFired);
        Assert.Equal(0, model.SomeNumber);
    }

    [Fact]
    public async Task EditContextFieldChangedNotificationSentOnValidValueChange()
    {
        var model = new TestModel();
        var notifications = new List<FieldIdentifier>();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNumber,
            ValueChanged = value => model.SomeNumber = value,
            ValueExpression = () => model.SomeNumber,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.SomeNumber);

        rootComponent.EditContext.OnFieldChanged += (sender, args) =>
        {
            notifications.Add(args.FieldIdentifier);
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("42");

        Assert.NotEmpty(notifications);
        Assert.Contains(fieldIdentifier, notifications);
    }

    [Fact]
    public async Task ValidationMessagesUpdatedOnParsingError()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.SomeNumber);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("42");
        var validMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.Empty(validMessages);

        await inputComponent.SetCurrentValueAsStringAsync("invalid");
        var invalidMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(invalidMessages);
    }

    [Fact]
    public async Task SequentialValueChangesFireMultipleCallbacks()
    {
        var model = new TestModel();
        var callbackCount = 0;
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNumber,
            ValueChanged = value =>
            {
                callbackCount++;
                model.SomeNumber = value;
            },
            ValueExpression = () => model.SomeNumber,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("10");
        Assert.Equal(1, callbackCount);

        await inputComponent.SetCurrentValueAsStringAsync("20");
        Assert.Equal(2, callbackCount);

        await inputComponent.SetCurrentValueAsStringAsync("30");
        Assert.Equal(3, callbackCount);

        Assert.Equal(30, model.SomeNumber);
    }

    [Fact]
    public async Task InvalidInputAfterValidInputDoesNotClearModel()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNumber,
            ValueChanged = value => model.SomeNumber = value,
            ValueExpression = () => model.SomeNumber,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("42");
        Assert.Equal(42, model.SomeNumber);

        await inputComponent.SetCurrentValueAsStringAsync("invalid");
        Assert.Equal(42, model.SomeNumber);
    }

    [Fact]
    public async Task CustomAttributePreservationInRenderTree()
    {
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "data-testid", "my-number-input" },
                { "autocomplete", "off" }
            }
        };

        var componentId = await RenderAndGetComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var testIdAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "data-testid");

        var autocompleteAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "autocomplete");

        Assert.NotNull(testIdAttribute.AttributeName);
        Assert.NotNull(autocompleteAttribute.AttributeName);
        Assert.Equal("my-number-input", testIdAttribute.AttributeValue);
        Assert.Equal("off", autocompleteAttribute.AttributeValue);
    }

    [Fact]
    public async Task NameAttributeGeneratedCorrectly()
    {
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };

        var componentId = await RenderAndGetComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var nameAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "name");

        Assert.NotNull(nameAttribute.AttributeName);
        Assert.True(((string)nameAttribute.AttributeValue).Length > 0);
    }

    [Fact]
    public async Task ValueAttributeReflectsCurrentValue()
    {
        var model = new TestModel { SomeNumber = 123 };
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNumber,
            ValueExpression = () => model.SomeNumber,
        };

        var componentId = await RenderAndGetComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var valueAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "value");

        Assert.NotNull(valueAttribute.AttributeName);
        Assert.Equal("123", valueAttribute.AttributeValue);
    }

    [Fact]
    public async Task DefaultAttributesRenderedWithCustomAttributes()
    {
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "min", "0" },
                { "max", "100" }
            }
        };

        var componentId = await RenderAndGetComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "type");

        var stepAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "step");

        var minAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "min");

        var maxAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "max");

        Assert.NotNull(typeAttribute.AttributeName);
        Assert.NotNull(stepAttribute.AttributeName);
        Assert.NotNull(minAttribute.AttributeName);
        Assert.NotNull(maxAttribute.AttributeName);
        Assert.Equal("number", typeAttribute.AttributeValue);
        Assert.Equal("any", stepAttribute.AttributeValue);
        Assert.Equal("0", minAttribute.AttributeValue);
        Assert.Equal("100", maxAttribute.AttributeValue);
    }

    [Fact]
    public async Task MultipleCustomAttributesCoexistWithDefaultAttributes()
    {
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "placeholder", "Enter a number" },
                { "required", true },
                { "class", "custom-input" }
            }
        };

        var componentId = await RenderAndGetComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);
        var attributes = frames.Array.Where(f => f.FrameType == RenderTreeFrameType.Attribute).ToList();

        Assert.NotEmpty(attributes);

        var placeholderAttr = attributes.Single(f => f.AttributeName == "placeholder");
        var classAttr = attributes.Single(f => f.AttributeName == "class");

        Assert.Equal("Enter a number", placeholderAttr.AttributeValue);
        Assert.Contains("custom-input", (string)classAttr.AttributeValue);
    }

    [Fact]
    public void FormatParseFormatRoundTrip()
    {
        var value = 123.456f;

        var component = new StandaloneInputNumber<float>();
        component.SetFieldName("Number");

        var formatted = component.FormatValue(value);

        var success = component.TryParseValue(formatted, out var parsed, out _);

        var reformatted = component.FormatValue(parsed);

        Assert.True(success);
        Assert.Equal(formatted, reformatted);
    }

    [Fact]
    public void ParseFormatRoundTrip()
    {
        var input = "456.789";

        var component = new StandaloneInputNumber<double>();
        component.SetFieldName("Number");

        var parseSuccess = component.TryParseValue(input, out var parsed, out _);

        var formatted = component.FormatValue(parsed);

        var reparseSuccess = component.TryParseValue(formatted, out var reparsed, out _);

        Assert.True(parseSuccess);
        Assert.True(reparseSuccess);
        Assert.Equal(parsed, reparsed);
    }

    [Fact]
    public async Task MultipleSequentialEdits()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNumber,
            ValueChanged = value => model.SomeNumber = value,
            ValueExpression = () => model.SomeNumber,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.SomeNumber);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("10");
        Assert.Equal(10, model.SomeNumber);
        Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));

        await inputComponent.SetCurrentValueAsStringAsync("20");
        Assert.Equal(20, model.SomeNumber);
        Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));

        await inputComponent.SetCurrentValueAsStringAsync("30");
        Assert.Equal(30, model.SomeNumber);
        Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
    }

    [Fact]
    public void DecimalRoundTripConsistency()
    {
        var values = new decimal[] { 0m, 1m, -1m, 99.99m, decimal.MaxValue / 2 };
        var component = new StandaloneInputNumber<decimal>();
        component.SetFieldName("DecimalTest");

        foreach (var value in values)
        {
            var formatted1 = component.FormatValue(value);
            var parseSuccess = component.TryParseValue(formatted1, out var parsed, out _);
            var formatted2 = component.FormatValue(parsed);

            Assert.True(parseSuccess);
            Assert.Equal(formatted1, formatted2);
            Assert.Equal(value, parsed);
        }
    }

    [Fact]
    public async Task CssClassesAddedForValidationState()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.SomeNumber);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalid");

        var componentId = await RenderAndGetComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "class");

        Assert.True(classAttribute.AttributeName == "class");
        var classValue = (string)classAttribute.AttributeValue;
        Assert.NotEmpty(classValue);
    }

    [Fact]
    public async Task CssValidClassAddedOnValidInput()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            Value = model.SomeNumber,
            ValueChanged = value => model.SomeNumber = value,
            ValueExpression = () => model.SomeNumber,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("42");

        var componentId = await RenderAndGetComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "class");

        Assert.True(classAttribute.AttributeName == "class");
        var classValue = (string)classAttribute.AttributeValue;
        Assert.NotEmpty(classValue);
    }

    [Fact]
    public async Task StepAttributeCanBeOverridden()
    {
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "step", "5" }
            }
        };

        var componentId = await RenderAndGetComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var stepAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "step");

        Assert.True(stepAttribute.AttributeName == "step");
        Assert.Equal("5", stepAttribute.AttributeValue);
    }

    [Fact]
    public async Task MinMaxAttributesControlValueRange()
    {
        var model = new TestModel();
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "min", "0" },
                { "max", "100" }
            }
        };

        var componentId = await RenderAndGetComponentIdAsync(hostComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var minAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "min");

        var maxAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "max");

        Assert.True(minAttribute.AttributeName == "min");
        Assert.True(maxAttribute.AttributeName == "max");
        Assert.Equal("0", minAttribute.AttributeValue);
        Assert.Equal("100", maxAttribute.AttributeValue);
    }

    [Fact]
    public void ParsesNegativeIntegerValue()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("NegativeInt");

        var success = component.TryParseValue("-42", out var result, out _);

        Assert.True(success);
        Assert.Equal(-42, result);
    }

    [Fact]
    public void ParsesNegativeDecimalValue()
    {
        var component = new StandaloneInputNumber<decimal>();
        component.SetFieldName("NegativeDecimal");

        var success = component.TryParseValue("-99.99", out var result, out _);

        Assert.True(success);
        Assert.Equal(-99.99m, result);
    }

    [Fact]
    public void HandlesZeroValue()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("ZeroValue");

        var success = component.TryParseValue("0", out var result, out _);

        Assert.True(success);
        Assert.Equal(0, result);
    }

    public static IEnumerable<object[]> DecimalBoundaryValues =>
    new List<object[]>
    {
        new object[] { decimal.MinValue, "decimal.MinValue" },
        new object[] { decimal.MaxValue, "decimal.MaxValue" }
    };

    private async Task<int> RenderAndGetComponentIdAsync<TValue, TComponent>(TestInputHostComponent<TValue, TComponent> hostComponent)
        where TComponent : InputBase<TValue>
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<TComponent>().Single().ComponentId;
    }

    private class TestModel
    {
        public int SomeNumber { get; set; }
    }

    private class NullableTestModel
    {
        public int? SomeNullableNumber { get; set; } = 123;
    }

    private class GuidTestModel
    {
        public Guid SomeGuid { get; set; }
    }

    private abstract class TestInputNumberComponentBase<TValue> : InputNumber<TValue>
    {
        public Task SetCurrentValueAsStringAsync(string value)
        {
            return InvokeAsync(() =>
            {
                base.CurrentValueAsString = value;
            });
        }
    }

    private class TestInputNumberComponent : TestInputNumberComponentBase<int>
    {
    }

    private class NullableTestInputNumberComponent : TestInputNumberComponentBase<int?>
    {
    }

    private class GuidTestInputNumberComponent : TestInputNumberComponentBase<Guid>
    {
    }

    private static string FormatValue<TValue>(TValue value)
    {
        var component = new StandaloneInputNumber<TValue>();
        return component.FormatValue(value);
    }

    private sealed class StandaloneInputNumber<TValue> : InputNumber<TValue>
    {
        public string FormatValue(TValue value) => base.FormatValueAsString(value);

        public bool TryParseValue(string value, out TValue result, out string validationErrorMessage)
            => base.TryParseValueFromString(value, out result, out validationErrorMessage);

        public void SetFieldName(string fieldName)
            => FieldIdentifier = new FieldIdentifier(new object(), fieldName);
    }
}
