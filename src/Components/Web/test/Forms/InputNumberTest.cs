// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

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

    [Fact]
    public async Task RendersIdAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.SomeNumber,
        };

        var componentId = await RenderAndGetTestInputNumberComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
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

        var componentId = await RenderAndGetTestInputNumberComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("custom-number-id", idAttribute.AttributeValue);
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

    [Theory]
    [InlineData("1e-6")]
    [InlineData("2E-06")]
    [InlineData("3.5e10")]
    [InlineData("-4.2E-03")]
    [InlineData("4E+8")]
    public async Task InputNumber_Double_AcceptsScientificNotation(string input)
    {
        var model = new TestModelDouble();
        var rootComponent = new TestInputHostComponent<double, TestInputNumberComponentDouble>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Value,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync(input);

        var fieldIdentifier = FieldIdentifier.Create(() => model.Value);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.Empty(validationMessages);
    }

    [Theory]
    [InlineData("1e-6")]
    [InlineData("2E-06")]
    [InlineData("3.5e4")]
    [InlineData("-4.25E-03")]
    [InlineData("4E+8")]
    public async Task InputNumber_Float_AcceptsScientificNotation(string input)
    {
        var model = new TestModelFloat();
        var rootComponent = new TestInputHostComponent<float, TestInputNumberComponentFloat>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Value,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync(input);

        var fieldIdentifier = FieldIdentifier.Create(() => model.Value);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.Empty(validationMessages);
    }

    [Theory]
    [InlineData("1e-6")]
    [InlineData("2E-06")]
    [InlineData("")]
    [InlineData(null)]
    public async Task InputNumber_NullableDouble_AcceptsScientificNotationAndEmpty(string? input)
    {
        var model = new TestModelNullableDouble();
        var rootComponent = new TestInputHostComponent<double?, TestInputNumberComponentNullableDouble>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Value,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync(input ?? "");

        var fieldIdentifier = FieldIdentifier.Create(() => model.Value);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.Empty(validationMessages);
    }

    [Theory]
    [InlineData("1e-6")]
    [InlineData("2E-06")]
    [InlineData("")]
    [InlineData(null)]
    public async Task InputNumber_NullableFloat_AcceptsScientificNotationAndEmpty(string? input)
    {
        var model = new TestModelNullableFloat();
        var rootComponent = new TestInputHostComponent<float?, TestInputNumberComponentNullableFloat>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Value,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputComponent.SetCurrentValueAsStringAsync(input ?? "");

        var fieldIdentifier = FieldIdentifier.Create(() => model.Value);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.Empty(validationMessages);
    }

    [Theory]
    [InlineData("2E")]
    [InlineData("2E-")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    public async Task InputNumber_Double_RejectsInvalidScientificNotation(string input)
    {
        var model = new TestModelDouble { Value = 1.0 }; // Set initial value
        var rootComponent = new TestInputHostComponent<double, TestInputNumberComponentDouble>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Value,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        var initialValue = model.Value;

        await inputComponent.SetCurrentValueAsStringAsync(input);

        var fieldIdentifier = FieldIdentifier.Create(() => model.Value);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("must be a number", validationMessages.First());
        Assert.Equal(initialValue, model.Value); // Value should remain unchanged
    }

    [Theory]
    [InlineData("2E")]
    [InlineData("2E-")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    public async Task InputNumber_Float_RejectsInvalidScientificNotation(string input)
    {
        var model = new TestModelFloat { Value = 1.0f }; // Set initial value
        var rootComponent = new TestInputHostComponent<float, TestInputNumberComponentFloat>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.Value,
        };
        var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        var initialValue = model.Value;

        await inputComponent.SetCurrentValueAsStringAsync(input);

        var fieldIdentifier = FieldIdentifier.Create(() => model.Value);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("must be a number", validationMessages.First());
        Assert.Equal(initialValue, model.Value); // Value should remain unchanged
    }

    private class TestModelDouble
    {
        public double Value { get; set; }
    }

    private class TestModelFloat
    {
        public float Value { get; set; }
    }

    private class TestModelNullableDouble
    {
        public double? Value { get; set; }
    }

    private class TestModelNullableFloat
    {
        public float? Value { get; set; }
    }

    private class TestInputNumberComponentDouble : InputNumber<double>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputNumberComponentFloat : InputNumber<float>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputNumberComponentNullableDouble : InputNumber<double?>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

    private class TestInputNumberComponentNullableFloat : InputNumber<float?>
    {
        public async Task SetCurrentValueAsStringAsync(string value)
        {
            await InvokeAsync(() => { base.CurrentValueAsString = value; });
        }
    }

}
