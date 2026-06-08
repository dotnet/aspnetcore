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

    // Type-Specific Boundary Tests
    [Fact]
    public void ParsesIntMinValue()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("IntMin");

        var success = component.TryParseValue(int.MinValue.ToString(CultureInfo.InvariantCulture), out var result, out _);

        Assert.True(success);
        Assert.Equal(int.MinValue, result);
    }

    [Fact]
    public void ParsesIntMaxValue()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("IntMax");

        var success = component.TryParseValue(int.MaxValue.ToString(CultureInfo.InvariantCulture), out var result, out _);

        Assert.True(success);
        Assert.Equal(int.MaxValue, result);
    }

    [Fact]
    public void ParsesLongMinValue()
    {
        var component = new StandaloneInputNumber<long>();
        component.SetFieldName("LongMin");

        var success = component.TryParseValue(long.MinValue.ToString(CultureInfo.InvariantCulture), out var result, out _);

        Assert.True(success);
        Assert.Equal(long.MinValue, result);
    }

    [Fact]
    public void FormatsIntMinValue()
    {
        var formatted = FormatValue(int.MinValue);
        Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Fact]
    public void FormatsIntMaxValue()
    {
        var formatted = FormatValue(int.MaxValue);
        Assert.Equal(int.MaxValue.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Fact]
    public void FormatsLongMinValue()
    {
        var formatted = FormatValue(long.MinValue);
        Assert.Equal(long.MinValue.ToString(CultureInfo.InvariantCulture), formatted);
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

    [Fact]
    public void ParsesDecimalMinValue()
    {
        var component = new StandaloneInputNumber<decimal>();
        component.SetFieldName("DecimalMin");

        var success = component.TryParseValue(decimal.MinValue.ToString(CultureInfo.InvariantCulture), out var result, out _);

        Assert.True(success);
        Assert.Equal(decimal.MinValue, result);
    }

    [Fact]
    public void ParsesDecimalMaxValue()
    {
        var component = new StandaloneInputNumber<decimal>();
        component.SetFieldName("DecimalMax");

        var success = component.TryParseValue(decimal.MaxValue.ToString(CultureInfo.InvariantCulture), out var result, out _);

        Assert.True(success);
        Assert.Equal(decimal.MaxValue, result);
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

    [Fact]
    public void FormatsDecimalMinValue()
    {
        var formatted = FormatValue(decimal.MinValue);
        Assert.Equal(decimal.MinValue.ToString(CultureInfo.InvariantCulture), formatted);
    }

    [Fact]
    public void FormatsDecimalMaxValue()
    {
        var formatted = FormatValue(decimal.MaxValue);
        Assert.Equal(decimal.MaxValue.ToString(CultureInfo.InvariantCulture), formatted);
    }

    // Nullable Type Coverage
    [Fact]
    public void ParsesNullableFloatWithValidInput()
    {
        var component = new StandaloneInputNumber<float?>();
        component.SetFieldName("NullableFloat");

        var success = component.TryParseValue("3.14", out var result, out _);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(3.14f, result.Value, 2);
    }

    [Fact]
    public void ParsesNullableFloatWithEmptyString()
    {
        var component = new StandaloneInputNumber<float?>();
        component.SetFieldName("NullableFloat");

        var success = component.TryParseValue(string.Empty, out var result, out _);

        Assert.True(success);
        Assert.Null(result);
    }

    [Fact]
    public void FormatsNullableFloatWithValue()
    {
        var component = new StandaloneInputNumber<float?>();
        var formatted = component.FormatValue(3.14f);

        Assert.NotNull(formatted);
        Assert.Equal(BindConverter.FormatValue(3.14f, CultureInfo.InvariantCulture), formatted);
    }

    [Fact]
    public void FormatsNullableFloatAsNull()
    {
        var component = new StandaloneInputNumber<float?>();
        var formatted = component.FormatValue(null);

        Assert.Null(formatted);
    }

    [Fact]
    public void ParsesNullableDoubleWithValidInput()
    {
        var component = new StandaloneInputNumber<double?>();
        component.SetFieldName("NullableDouble");

        var success = component.TryParseValue("2.718", out var result, out _);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(2.718, result.Value, 3);
    }

    [Fact]
    public void ParsesNullableDecimalWithValidInput()
    {
        var component = new StandaloneInputNumber<decimal?>();
        component.SetFieldName("NullableDecimal");

        var success = component.TryParseValue("99.99", out var result, out _);

        Assert.True(success);
        Assert.Equal(99.99m, result);
    }

    [Fact]
    public void ParsesNullableLongWithValidInput()
    {
        var component = new StandaloneInputNumber<long?>();
        component.SetFieldName("NullableLong");

        var success = component.TryParseValue("123456789", out var result, out _);

        Assert.True(success);
        Assert.Equal(123456789L, result);
    }

    // Invalid Input Pattern Tests
    [Fact]
    public void RejectsWhitespaceOnlyInput()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("WhitespaceTest");

        var success = component.TryParseValue("   ", out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.NotNull(validationErrorMessage);
    }

    [Fact]
    public void AcceptsLeadingWhitespace()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("LeadingWhitespaceTest");

        var success = component.TryParseValue("  42", out var result, out _);

        Assert.True(success);
        Assert.Equal(42, result);
    }

    [Fact]
    public void AcceptsTrailingWhitespace()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("TrailingWhitespaceTest");

        var success = component.TryParseValue("42  ", out var result, out _);

        Assert.True(success);
        Assert.Equal(42, result);
    }

    [Fact]
    public void RejectsScientificNotation()
    {
        var component = new StandaloneInputNumber<double>();
        component.SetFieldName("ScientificNotationTest");

        var success = component.TryParseValue("1.5e10", out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.NotNull(validationErrorMessage);
    }

    [Fact]
    public void RejectsHexNotation()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("HexNotationTest");

        var success = component.TryParseValue("0x2A", out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.NotNull(validationErrorMessage);
    }

    [Fact]
    public void RejectsCurrencySymbol()
    {
        var component = new StandaloneInputNumber<decimal>();
        component.SetFieldName("CurrencyTest");

        var success = component.TryParseValue("$99.99", out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.NotNull(validationErrorMessage);
    }

    [Fact]
    public void AcceptsPlusSign()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("PlusSignTest");

        var success = component.TryParseValue("+42", out var result, out _);

        Assert.True(success);
        Assert.Equal(42, result);
    }

    [Fact]
    public void RejectsDoubleNegative()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("DoubleNegativeTest");

        var success = component.TryParseValue("--42", out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.NotNull(validationErrorMessage);
    }

    [Fact]
    public void RejectsMultipleDecimalPoints()
    {
        var component = new StandaloneInputNumber<double>();
        component.SetFieldName("MultipleDecimalTest");

        var success = component.TryParseValue("3.14.159", out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.NotNull(validationErrorMessage);
    }

    [Fact]
    public void RejectsEmptyStringForNonNullableType()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("NonNullableEmptyTest");

        var success = component.TryParseValue(string.Empty, out _, out var validationErrorMessage);

        Assert.False(success);
        Assert.NotNull(validationErrorMessage);
    }

    // Binding & Event Behavior Tests
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

        // First, set valid value - should have no errors
        await inputComponent.SetCurrentValueAsStringAsync("42");
        var validMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.Empty(validMessages);

        // Now set invalid value - should have errors
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

        // Set valid value
        await inputComponent.SetCurrentValueAsStringAsync("42");
        Assert.Equal(42, model.SomeNumber);

        // Try to set invalid value - model should retain previous value
        await inputComponent.SetCurrentValueAsStringAsync("invalid");
        Assert.Equal(42, model.SomeNumber);
    }

    // Attributes & Rendering Tests
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

        // Find custom attributes in render tree
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

        // Name attribute should exist and be properly formatted
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

        // Verify default attributes still present
        var typeAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "type");

        var stepAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "step");

        // Verify custom attributes present
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

        // Should have multiple attributes (type, step, id, name, value, plus custom ones)
        Assert.NotEmpty(attributes);

        // Check for some of our custom attributes
        var placeholderAttr = attributes.Single(f => f.AttributeName == "placeholder");
        var classAttr = attributes.Single(f => f.AttributeName == "class");

        Assert.Equal("Enter a number", placeholderAttr.AttributeValue);
        // Class attribute includes custom class plus validation classes from EditContext
        Assert.Contains("custom-input", (string)classAttr.AttributeValue);
    }

    // Integration & Polish Tests
    [Fact]
    public void FormatParseFormatRoundTrip()
    {
        var value = 123.456f;

        var component = new StandaloneInputNumber<float>();
        component.SetFieldName("Number");

        // Format
        var formatted = component.FormatValue(value);

        // Parse
        var success = component.TryParseValue(formatted, out var parsed, out _);

        // Format again
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

        // Parse
        var parseSuccess = component.TryParseValue(input, out var parsed, out _);

        // Format
        var formatted = component.FormatValue(parsed);

        // Parse again
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

        // Multiple edits in sequence
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
            // Format → Parse → Format
            var formatted1 = component.FormatValue(value);
            var parseSuccess = component.TryParseValue(formatted1, out var parsed, out _);
            var formatted2 = component.FormatValue(parsed);

            Assert.True(parseSuccess);
            Assert.Equal(formatted1, formatted2);
            Assert.Equal(value, parsed);
        }
    }

    // CSS Class & EditContext Tests
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

        // Set invalid value to trigger validation
        await inputComponent.SetCurrentValueAsStringAsync("invalid");

        var componentId = await RenderAndGetComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "class");

        // Verify the class attribute exists and has a value
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

        // Set valid value
        await inputComponent.SetCurrentValueAsStringAsync("42");

        var componentId = await RenderAndGetComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "class");

        // Verify the class attribute exists and has a value
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
                { "step", "5" }  // Override default step="any"
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
    public async Task ParsesNegativeIntegerValue()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("NegativeInt");

        var success = component.TryParseValue("-42", out var result, out _);

        Assert.True(success);
        Assert.Equal(-42, result);
    }

    [Fact]
    public async Task ParsesNegativeDecimalValue()
    {
        var component = new StandaloneInputNumber<decimal>();
        component.SetFieldName("NegativeDecimal");

        var success = component.TryParseValue("-99.99", out var result, out _);

        Assert.True(success);
        Assert.Equal(-99.99m, result);
    }

    [Fact]
    public async Task HandlesZeroValue()
    {
        var component = new StandaloneInputNumber<int>();
        component.SetFieldName("ZeroValue");

        var success = component.TryParseValue("0", out var result, out _);

        Assert.True(success);
        Assert.Equal(0, result);
    }

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
            // This is equivalent to the subclass writing to CurrentValueAsString
            // (e.g., from @bind), except to simplify the test code there's an InvokeAsync
            // here. In production code it wouldn't normally be required because @bind
            // calls run on the sync context anyway.
            return InvokeAsync(() => { base.CurrentValueAsString = value; });
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
