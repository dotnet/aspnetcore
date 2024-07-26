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

        // Retrieve the render tree frames and extract attributes
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Debugging: Output the frames for inspection
        foreach (var frame in frames.Array)
        {
            Console.WriteLine($"Frame: {frame.FrameType}, {frame.ElementName}, {frame.AttributeName}, {frame.AttributeValue}");
        }

        bool inputElementFound = false;
        var attributes = new Dictionary<string, object>();

        for (int i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "input")
            {
                inputElementFound = true;
                for (int j = i + 1; j < frames.Count; j++)
                {
                    var attributeFrame = frames.Array[j];
                    if (attributeFrame.FrameType != RenderTreeFrameType.Attribute)
                    {
                        break;
                    }
                    attributes[attributeFrame.AttributeName] = attributeFrame.AttributeValue;
                }
                break;
            }
        }

        // Assert
        Assert.True(inputElementFound, "Input element was not found.");
        Assert.True(attributes.ContainsKey("type"), "Type attribute was not found.");
        Assert.Equal("range", attributes["type"]);
    }

    private async Task<int> RenderAndGetTestInputNumberComponentIdAsync(TestInputHostComponent<int, TestInputNumberComponent> hostComponent)
    {
        var componentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(componentId);
        return FindTestInputNumberComponentId(_testRenderer.Batches.Single(), typeof(TestInputNumberComponent));
    }

    private static int FindTestInputNumberComponentId(CapturedBatch batch, Type componentType)
        => batch.ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Component && f.Component.GetType() == componentType)
                .Select(f => f.ComponentId)
                .Single();

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
