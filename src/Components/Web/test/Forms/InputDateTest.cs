// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class InputDateTest
{
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
        // DateTime defaults to Date for backward compatibility
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
    public async Task DateTimeDefaultsToDate()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        // Act
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert - DateTime should default to Date (Type is null, auto-detected)
        Assert.Null(inputComponent.Type);
    }

    [Fact]
    public async Task DateTimeOffsetDefaultsToDate()
    {
        // Arrange
        var model = new TestModelDateTimeOffset();
        var rootComponent = new TestInputHostComponent<DateTimeOffset, TestInputDateTimeOffsetComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        // Act
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert - DateTimeOffset should default to Date (Type is null, auto-detected)
        Assert.Null(inputComponent.Type);
    }

    [Fact]
    public async Task DateOnlyDefaultsToDate()
    {
        // Arrange
        var model = new TestModelDateOnly();
        var rootComponent = new TestInputHostComponent<DateOnly, TestInputDateOnlyComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };

        // Act
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert - DateOnly should default to Date (Type is null, auto-detected)
        Assert.Null(inputComponent.Type);
    }

    [Fact]
    public async Task TimeOnlyDefaultsToTime()
    {
        // Arrange
        var model = new TestModelTimeOnly();
        var rootComponent = new TestInputHostComponent<TimeOnly, TestInputTimeOnlyComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.TimeProperty,
        };

        // Act
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert - TimeOnly should default to Time (Type is null, auto-detected)
        Assert.Null(inputComponent.Type);
    }

    [Fact]
    public async Task ExplicitTypeOverridesAutoDetection()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
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

        // Act
        await inputComponent.SetCurrentValueAsStringAsync("invalidDate");

        // Assert - Explicitly set Type=DateTimeLocal should produce "date and time" error message
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The DateProperty field must be a date and time.", validationMessages);
    }

    [Fact]
    public async Task TimeOnlyValidationErrorMessage()
    {
        // Arrange
        var model = new TestModelTimeOnly();
        var rootComponent = new TestInputHostComponent<TimeOnly, TestInputTimeOnlyComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.TimeProperty,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.TimeProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputComponent.SetCurrentValueAsStringAsync("invalidTime");

        // Assert - TimeOnly should default to Time, so error message is "time"
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The TimeProperty field must be a time.", validationMessages);
    }

    [Fact]
    public async Task DateOnlyValidationErrorMessage()
    {
        // Arrange
        var model = new TestModelDateOnly();
        var rootComponent = new TestInputHostComponent<DateOnly, TestInputDateOnlyComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.DateProperty,
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputComponent.SetCurrentValueAsStringAsync("invalidDate");

        // Assert - DateOnly should default to Date, so error message is "date"
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The DateProperty field must be a date.", validationMessages);
    }

    private class TestModel
    {
        public DateTime DateProperty { get; set; }
    }

    private class TestModelDateTimeOffset
    {
        public DateTimeOffset DateProperty { get; set; }
    }

    private class TestModelDateOnly
    {
        public DateOnly DateProperty { get; set; }
    }

    private class TestModelTimeOnly
    {
        public TimeOnly TimeProperty { get; set; }
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

    private class TestInputDateTimeOffsetComponent : InputDate<DateTimeOffset>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputDateOnlyComponent : InputDate<DateOnly>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputTimeOnlyComponent : InputDate<TimeOnly>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }
}
