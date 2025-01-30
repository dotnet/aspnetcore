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

    [Theory]
    [InlineData("1.1", "0.0#", "1.1")]                      // Single decimal place with optional second
    [InlineData("1500", "0.00", "1500.00")]                 // Force two decimal places
    [InlineData("1500", "0.0000", "1500.0000")]             // Force four decimal places
    [InlineData("1500", "0.##", "1500")]                    // Remove unnecessary decimals
    [InlineData("0", "0.00", "0.00")]                       // Zero with fixed decimals
    [InlineData("0", "0.##", "0")]                          // Zero with optional decimals
    [InlineData("-1.1", "0.0#", "-1.1")]                    // Negative number with one decimal place
    [InlineData("-1500", "0.00", "-1500.00")]               // Negative number with two fixed decimals
    [InlineData("1.999", "0.0", "2.0")]                     // Rounding up
    [InlineData("1.111", "0.0", "1.1")]                     // Rounding down
    [InlineData("1234567.89", "N2", "1,234,567.89")]        // Large number with thousands separator
    [InlineData("1234567.89", "#,##0.00", "1,234,567.89")]  // Explicit thousands separator format
    [InlineData("0.1234", "0.00%", "12.34%")]               // Percentage formatting
    [InlineData("0.12", "00.00", "00.12")]                  // Fixed zero's with fixed decimals
    [InlineData("1234567.89", "0.00", "1234567.89")]        // Fixed two decimals
    public async Task FormatDoubles(string value, string format, string expected)
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<double, TestInputNumberComponent<double>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Double,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "Format", format }
            }
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        inputComponent.CurrentValueAsString = value;

        // Assert
        Assert.Equal(expected, inputComponent.CurrentValueAsString);
    }

    [Fact]
    public async Task ValidationErrorUsesDisplayAttributeName()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Int,
            AdditionalAttributes = new Dictionary<string, object>
            {
                { "DisplayName", "Some number" }
            }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.Int);
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
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Int,
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
        var hostComponent = new TestInputHostComponent<int, TestInputNumberComponent<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Int,
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

    private async Task<int> RenderAndGetTestInputNumberComponentIdAsync(TestInputHostComponent<int, TestInputNumberComponent<int>> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<TestInputNumberComponent<int>>().Single().ComponentId;
    }

    private class TestModel
    {
        public int Int { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public decimal Decimal { get; set; }
    }

    class TestInputNumberComponent<TValue> : InputNumber<TValue>
    {
        public new TValue CurrentValue => base.CurrentValue;

        public new string CurrentValueAsString
        {
            get => base.CurrentValueAsString;
            set => base.CurrentValueAsString = value;
        }
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
