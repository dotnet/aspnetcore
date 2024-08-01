// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Infrastructure;
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
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert
        Assert.NotNull(inputSelectComponent.Element);
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
        var componentId = await RenderAndGetTestInputNumberComponentIdAsync(hostComponent);

        // Retrieve the render tree frames and extract attributes using helper methods
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var typeAttributeFrame = frames.Array.Single(frame =>
            frame.FrameType == RenderTreeFrameType.Attribute &&
            frame.AttributeName == "type");

        // Assert
        Assert.Equal("range", typeAttributeFrame.AttributeValue);
    }

    private async Task<int> RenderAndGetTestInputNumberComponentIdAsync(TestInputHostComponent<int, TestInputNumberComponent> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<TestInputNumberComponent>().Single().ComponentId;
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
