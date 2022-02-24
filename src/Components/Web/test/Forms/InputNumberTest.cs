// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class InputNumberTest
{
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
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert
        Assert.NotNull(inputSelectComponent.Element);
    }

    private class TestModel
    {
        public int SomeNumber { get; set; }
    }

    private class TestInputNumberComponent : InputNumber<int>
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
}
