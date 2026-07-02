// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputDateTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

    [Fact]
    public async Task ValidationErrorUsesDisplayAttributeName()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "DisplayName", "Date property" }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputComponent.SetCurrentValueAsStringAsync("invalidDate");

        // Assert
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The Date property field must be a date.", validationMessages);
    }

    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        // Act
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert
        Assert.NotNull(inputSelectComponent.Element);
    }

    [Fact]
    public async Task RendersIdAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        var componentId = await RenderAndGetInputDateComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("model_DateProperty", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task ExplicitIdOverridesGenerated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object> { { "id", "custom-date-id" } }
        };

        var componentId = await RenderAndGetInputDateComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("custom-date-id", idAttribute.AttributeValue);
    }

    // Test DateOnly support
    [Fact]
    public async Task ValidationErrorUsesDisplayAttributeName_DateOnly()
    {
        var model = new TestModelDateOnly();
        var rootComponent = new TestInputHostComponent<DateOnly, TestInputDateDateOnlyComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "DisplayName", "Date only property" }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalidDate");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The Date only property field must be a date.", validationMessages);
    }

    // Test InputDateType combinations with DateTime
    [Fact]
    public async Task ValidationErrorWithDateTimeLocalType()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponentWithType>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "Type", InputDateType.DateTimeLocal }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalidDateTime");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The DateProperty field must be a date and time.", validationMessages);
    }

    [Fact]
    public async Task ValidationErrorWithMonthType()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponentWithType>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "Type", InputDateType.Month }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalidMonth");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The DateProperty field must be a year and month.", validationMessages);
    }

    [Fact]
    public async Task ValidationErrorWithTimeType()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponentWithType>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "Type", InputDateType.Time }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalidTime");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The DateProperty field must be a time.", validationMessages);
    }

    // Test InputDateType with nullable types
    [Fact]
    public async Task ValidationErrorWithDateTimeLocalType_Nullable()
    {
        var model = new TestModelNullable();
        var rootComponent = new TestInputHostComponent<DateTime?, TestInputDateNullableComponentWithType>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "Type", InputDateType.DateTimeLocal }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalidDateTime");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The DateProperty field must be a date and time.", validationMessages);
    }

    // Test DateTimeOffset with different InputDateType
    [Fact]
    public async Task DateTimeOffsetWithMonthType()
    {
        var model = new TestModelDateTimeOffset();
        var rootComponent = new TestInputHostComponent<DateTimeOffset, TestInputDateDateTimeOffsetComponentWithType>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "Type", InputDateType.Month }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalidMonth");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The DateProperty field must be a year and month.", validationMessages);
    }

    // Test DateOnly with different InputDateType
    [Fact]
    public async Task DateOnlyWithTimeType()
    {
        var model = new TestModelDateOnly();
        var rootComponent = new TestInputHostComponent<DateOnly, TestInputDateDateOnlyComponentWithType>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "Type", InputDateType.Time }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalidTime");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The DateProperty field must be a time.", validationMessages);
    }

    [Fact]
    public async Task RendersTypeAttributeAsTime()
    {
        var model = new TestModelTimeOnly();
        var rootComponent = new TestInputHostComponent<TimeOnly, TestInputDateTimeOnlyComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.TimeProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "Type", InputDateType.Time }
            }
        };

        var hostId = _testRenderer.AssignRootComponentId(rootComponent);
        await _testRenderer.RenderRootComponentAsync(hostId);
        var batch = _testRenderer.Batches.Single();
        var componentFrame = batch.GetComponentFrames<TestInputDateTimeOnlyComponent>().Single();
        var componentId = componentFrame.ComponentId;
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type");
        Assert.Equal("time", typeAttribute.AttributeValue);
    }

    [Fact]
    public async Task UsesCustomParsingErrorMessage()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "ParsingErrorMessage", "My custom parse error for {0}." }
            }
        };

        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalidDate");

        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("My custom parse error for DateProperty.", validationMessages);
    }

    [Fact]
    public async Task ValueChangedCallbackInvokedWhenValueChanges()
    {
        var model = new TestModel();
        DateTime? captured = null;
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            ValueChanged = v => captured = v
        };

        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("2020-01-01");

        Assert.NotNull(captured);
        Assert.Equal(new DateTime(2020, 1, 1), captured.Value);
    }

    [Fact]
    public async Task CssClassIncludesFieldValidationState()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        var componentId = await RenderAndGetInputDateComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var classAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Contains("valid", classAttribute.AttributeValue.ToString());
    }

    [Fact]
    public async Task RendersNameAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        var componentId = await RenderAndGetInputDateComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var nameAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "name");
        Assert.False(string.IsNullOrEmpty(nameAttribute.AttributeValue?.ToString()));
    }

    [Fact]
    public async Task AdditionalAttributesArePropagated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "placeholder", "Enter date" },
                { "data-test", "input-date" }
            }
        };

        var hostComponentId = _testRenderer.AssignRootComponentId(rootComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var componentId = _testRenderer.Batches.Last().GetComponentFrames<TestInputDateComponent>().Single().ComponentId;
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var placeholder = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "placeholder");
        var dataTest = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "data-test");

        Assert.Equal("Enter date", placeholder.AttributeValue);
        Assert.Equal("input-date", dataTest.AttributeValue);
    }
    [Fact]
    public async Task ValidationMessageClearedAfterValidInput()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync("invalid");

        Assert.NotEmpty(
            rootComponent.EditContext.GetValidationMessages(fieldIdentifier));

        await inputComponent.SetCurrentValueAsStringAsync("2024-01-01");

        Assert.Empty(
            rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
    }
    [Fact]
    public async Task RendersTypeAttributeAsDateTimeLocal()
    {
        var model = new TestModel();

        var rootComponent =
            new TestInputHostComponent<DateTime,
                TestInputDateComponentWithType>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.DateProperty,
                AdditionalAttributes = new Dictionary<string, object>
                {
                { "Type", InputDateType.DateTimeLocal }
                }
            };

        var hostId = _testRenderer.AssignRootComponentId(rootComponent);

        await _testRenderer.RenderRootComponentAsync(hostId);

        var batch = _testRenderer.Batches.Single();

        var componentFrame =
            batch.GetComponentFrames<TestInputDateComponentWithType>()
                 .Single();

        var frames =
            _testRenderer.GetCurrentRenderTreeFrames(componentFrame.ComponentId);

        var typeAttribute =
            frames.Array.Single(
                f => f.FrameType == RenderTreeFrameType.Attribute &&
                     f.AttributeName == "type");

        Assert.Equal("datetime-local", typeAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersTypeAttributeAsDate()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        var hostId = _testRenderer.AssignRootComponentId(rootComponent);

        await _testRenderer.RenderRootComponentAsync(hostId);

        var batch = _testRenderer.Batches.Single();

        var componentFrame =
            batch.GetComponentFrames<TestInputDateComponent>().Single();

        var frames =
            _testRenderer.GetCurrentRenderTreeFrames(componentFrame.ComponentId);

        var typeAttribute =
            frames.Array.Single(
                f => f.FrameType == RenderTreeFrameType.Attribute &&
                     f.AttributeName == "type");

        Assert.Equal("date", typeAttribute.AttributeValue);
    }

    [Fact]
    public async Task RendersTypeAttributeAsMonth()
    {
        var model = new TestModel();

        var rootComponent =
            new TestInputHostComponent<DateTime, TestInputDateComponentWithType>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.DateProperty,
                AdditionalAttributes = new Dictionary<string, object>
                {
                { "Type", InputDateType.Month }
                }
            };

        var hostId = _testRenderer.AssignRootComponentId(rootComponent);

        await _testRenderer.RenderRootComponentAsync(hostId);

        var batch = _testRenderer.Batches.Single();

        var componentFrame =
            batch.GetComponentFrames<TestInputDateComponentWithType>()
                 .Single();

        var frames =
            _testRenderer.GetCurrentRenderTreeFrames(componentFrame.ComponentId);

        var typeAttribute =
            frames.Array.Single(
                f => f.FrameType == RenderTreeFrameType.Attribute &&
                     f.AttributeName == "type");

        Assert.Equal("month", typeAttribute.AttributeValue);
    }
    [Fact]
    public void ThrowsForUnsupportedType()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new TestInputDateUnsupportedComponent());

        Assert.Contains("Unsupported", ex.Message);
    }
    private class TestInputDateUnsupportedComponent : InputDate<int>
    {
    }
    private async Task<int> RenderAndGetInputDateComponentIdAsync(TestInputHostComponent<DateTime, TestInputDateComponent> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<TestInputDateComponent>().Single().ComponentId;
    }

    private class TestModel
    {
        public DateTime DateProperty { get; set; }
    }

    private class TestModelNullable
    {
        public DateTime? DateProperty { get; set; }
    }

    private class TestModelDateTimeOffset
    {
        public DateTimeOffset DateProperty { get; set; }
    }

    private class TestModelNullableDateTimeOffset
    {
        public DateTimeOffset? DateProperty { get; set; }
    }

    private class TestModelDateOnly
    {
        public DateOnly DateProperty { get; set; }
    }

    private class TestModelNullableDateOnly
    {
        public DateOnly? DateProperty { get; set; }
    }

    private class TestModelTimeOnly
    {
        public TimeOnly TimeProperty { get; set; }
    }

    private class TestModelNullableTimeOnly
    {
        public TimeOnly? TimeProperty { get; set; }
    }

    private class TestInputDateComponent : InputDate<DateTime>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            // This is equivalent to the subclass writing to CurrentValueAsString
            // (e.g., from @bind), except to simplify the test code there's an InvokeAsync
            // here. In production code it wouldn't normally be required because @bind
            // calls run on the sync context anyway.
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateNullableComponent : InputDate<DateTime?>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateDateTimeOffsetComponent : InputDate<DateTimeOffset>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateNullableDateTimeOffsetComponent : InputDate<DateTimeOffset?>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateDateOnlyComponent : InputDate<DateOnly>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateNullableDateOnlyComponent : InputDate<DateOnly?>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateTimeOnlyComponent : InputDate<TimeOnly>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateNullableTimeOnlyComponent : InputDate<TimeOnly?>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateComponentWithType : InputDate<DateTime>
    {

        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateNullableComponentWithType : InputDate<DateTime?>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateDateTimeOffsetComponentWithType : InputDate<DateTimeOffset>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateDateOnlyComponentWithType : InputDate<DateOnly>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

}
