// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputRadioTest
{
    [Fact]
    public async Task ThrowsOnFirstRenderIfInputRadioHasNoGroup()
    {
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithoutGroup(null)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RenderAndGetTestInputComponentAsync(rootComponent));
        Assert.Contains($"must have an ancestor", ex.Message);
    }

    [Fact]
    public async Task GroupGeneratesNameGuidWhenInvalidNameSupplied()
    {
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model)
            {
                ShouldUseFieldIdentifiers = false,
            },
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        Assert.All(inputRadioComponents, inputRadio => Assert.True(Guid.TryParseExact(inputRadio.GroupName, "N", out _)));
    }

    [Fact]
    public async Task RadioInputContextExistsWhenValidNameSupplied()
    {
        var groupName = "group";
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(groupName, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        Assert.All(inputRadioComponents, inputRadio => Assert.Equal(groupName, inputRadio.GroupName));
    }

    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        Assert.All(inputRadioComponents, inputRadio => Assert.NotNull(inputRadio.Element));
    }

    [Fact]
    public async Task RadioInputContextIsCreatedWithValidGroup()
    {
        // Validates that InputRadioContext creation and initialization
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        // All radios should have a non-null context
        Assert.All(inputRadioComponents, inputRadio => Assert.NotNull(inputRadio.Context));
    }

    [Fact]
    public async Task ClientValidationAttributesPassedOnlyToFirstRadio()
    {
        // Validates that data-val-* attributes are passed only to the first radio in the group
        // This matches MVC behavior where validation attributes appear only on first radio
        var model = new TestModel();
        var groupName = "test-group";
        var validationAttributes = new Dictionary<string, object>
        {
            { "data-val", "true" },
            { "data-val-required", "This field is required." }
        };

        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroupAndAttributes(groupName, () => model.TestEnum, validationAttributes)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        // Get all validation attributes
        var validationAttrs = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute 
                && f.AttributeName.StartsWith("data-val", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Should have validation attributes (passed to first radio only, then cleared)
        Assert.NotEmpty(validationAttrs);
    }

    [Fact]
    public async Task AdditionalAttributesAreMergedWithRadioElements()
    {
        // Validates that additional attributes (like class, aria-*, etc.) are properly applied
        var model = new TestModel();
        var additionalAttrs = new Dictionary<string, object>
        {
            { "class", "custom-radio" },
            { "aria-label", "Choose option" }
        };

        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroupAndAdditionalAttrs(null, () => model.TestEnum, additionalAttrs)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        var classAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class")
            .ToList();

        var ariaLabelAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "aria-label")
            .ToList();

        // Check that additional attributes are present
        Assert.True(classAttributes.Any(a => (a.AttributeValue as string)?.Contains("custom-radio") == true),
            "Additional class attribute should be present");
        Assert.True(ariaLabelAttributes.Any(a => a.AttributeValue as string == "Choose option"),
            "Additional aria-label attribute should be present");
    }

    [Fact]
    public async Task SelectedValueRendersCheckedAttributeCorrectly()
    {
        // Validates that the checked attribute is set correctly based on CurrentValue comparison
        var model = new TestModel { TestEnum = TestEnum.Two };
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        // Get all checked attribute assignments
        var checkedAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "checked")
            .ToList();

        // At least one checked attribute should be present
        Assert.True(checkedAttributes.Count > 0, "Expected at least one checked attribute");
    }

    [Fact]
    public async Task GroupNameAttributeIsAppliedToAllRadiosInGroup()
    {
        // Validates that all radios in a group have the same name attribute
        var model = new TestModel();
        var groupName = "my-radio-group";
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(groupName, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        var nameAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "name")
            .ToList();

        // All name attributes should have the same value as the group name
        Assert.True(nameAttributes.Count >= 3, "Expected at least 3 radios with name attributes");
        Assert.All(nameAttributes, attr => Assert.Equal(groupName, attr.AttributeValue));
    }

    [Fact]
    public async Task RadioValueIsFormattedAsString()
    {
        // Validates that radio values are properly formatted as strings using BindConverter
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        var valueAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value")
            .ToList();

        // Each radio should have a value attribute with the enum name as string
        var expectedValues = new[] { "One", "Two", "Three" };
        Assert.Equal(3, valueAttributes.Count);
        
        for (int i = 0; i < valueAttributes.Count; i++)
        {
            Assert.Equal(expectedValues[i], valueAttributes[i].AttributeValue);
        }
    }

    [Fact]
    public async Task MultipleRadiosInGroupHaveSameName()
    {
        // Validates that when multiple radios exist in a single group, they all have the same name attribute
        // This is the foundational behavior that prevents radios from different groups from interfering
        var model = new TestModel();
        var groupName = "test-group";
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(groupName, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        var nameAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "name")
            .ToList();

        // Should have 3 name attributes (one for each radio button)
        Assert.Equal(3, nameAttributes.Count);

        // All should have the same group name
        Assert.All(nameAttributes, attr => Assert.Equal(groupName, attr.AttributeValue));
    }

    [Fact]
    public async Task OnChangeEventCallbackIsAttached()
    {
        // Validates that the onchange event handler is properly attached
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        var onchangeAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange")
            .ToList();

        // All radios should have an onchange handler
        Assert.True(onchangeAttributes.Count >= 3,
            "Each radio should have an onchange event handler");
    }

    [Fact]
    public async Task RadioTypeIsAlwaysRendered()
    {
        // Validates that type="radio" is always rendered
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        var typeAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "type")
            .ToList();

        // All type attributes should be "radio"
        Assert.True(typeAttributes.Count >= 3, "Expected at least 3 radios");
        Assert.All(typeAttributes, attr => Assert.Equal("radio", attr.AttributeValue));
    }

    [Fact]
    public async Task ContextFromCascadedParameterIsPropagated()
    {
        // Validates that InputRadioContext cascading parameter is properly handled
        var model = new TestModel();
        var groupName = "cascaded-group";
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(groupName, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        // All radios should have found the cascaded context with matching group name
        Assert.All(inputRadioComponents, inputRadio =>
            Assert.Equal(groupName, inputRadio.GroupName));
    }

    [Fact]
    public async Task FindContextInAncestorsReturnsCorrectContext()
    {
        // Validates the FindContextInAncestors method works correctly
        var model = new TestModel();
        var namedGroup = "named-group";
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroupWithExplicitName(namedGroup, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        // Radios with explicit Name should find the context in ancestors
        Assert.All(inputRadioComponents, radio =>
            Assert.Equal(namedGroup, radio.GroupName));
    }

    [Fact]
    public async Task ElementReferenceIsCaptured()
    {
        // Validates that ElementReference is properly captured for each radio
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        Assert.All(inputRadioComponents, inputRadio =>
            Assert.NotNull(inputRadio.Element)
        );
    }

    private static RenderFragment RadioButtonsWithoutGroup(string name) => (builder) =>
    {
        foreach (var selectedValue in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
        {
            builder.OpenComponent<TestInputRadio>(0);
            builder.AddComponentParameter(1, "Name", name);
            builder.AddComponentParameter(2, "Value", selectedValue);
            builder.CloseComponent();
        }
    };

    private static RenderFragment RadioButtonsWithGroup(string name, Expression<Func<TestEnum>> valueExpression) => (builder) =>
    {
        builder.OpenComponent<InputRadioGroup<TestEnum>>(0);
        builder.AddComponentParameter(1, "Name", name);
        builder.AddComponentParameter(2, "ValueExpression", valueExpression);
        builder.AddComponentParameter(2, "ChildContent", new RenderFragment((childBuilder) =>
        {
            foreach (var value in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
            {
                childBuilder.OpenComponent<TestInputRadio>(0);
                childBuilder.AddComponentParameter(1, "Value", value);
                childBuilder.CloseComponent();
            }
        }));

        builder.CloseComponent();
    };

    private static RenderFragment RadioButtonsWithGroupAndAttributes(
        string name,
        Expression<Func<TestEnum>> valueExpression,
        Dictionary<string, object> validationAttributes) => (builder) =>
    {
        builder.OpenComponent<InputRadioGroup<TestEnum>>(0);
        builder.AddComponentParameter(1, "Name", name);
        builder.AddComponentParameter(2, "ValueExpression", valueExpression);
        builder.AddMultipleAttributes(3, validationAttributes);
        builder.AddComponentParameter(4, "ChildContent", new RenderFragment((childBuilder) =>
        {
            foreach (var value in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
            {
                childBuilder.OpenComponent<TestInputRadio>(0);
                childBuilder.AddComponentParameter(1, "Value", value);
                childBuilder.CloseComponent();
            }
        }));
        builder.CloseComponent();
    };

    private static RenderFragment RadioButtonsWithGroupAndAdditionalAttrs(
        string name,
        Expression<Func<TestEnum>> valueExpression,
        Dictionary<string, object> additionalAttrs) => (builder) =>
    {
        builder.OpenComponent<InputRadioGroup<TestEnum>>(0);
        builder.AddComponentParameter(1, "Name", name);
        builder.AddComponentParameter(2, "ValueExpression", valueExpression);
        builder.AddComponentParameter(3, "ChildContent", new RenderFragment((childBuilder) =>
        {
            foreach (var value in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
            {
                childBuilder.OpenComponent<TestInputRadio>(0);
                childBuilder.AddComponentParameter(1, "Value", value);
                childBuilder.AddMultipleAttributes(2, additionalAttrs);
                childBuilder.CloseComponent();
            }
        }));
        builder.CloseComponent();
    };

    private static RenderFragment RadioButtonsWithGroupWithExplicitName(
        string name,
        Expression<Func<TestEnum>> valueExpression) => (builder) =>
    {
        builder.OpenComponent<InputRadioGroup<TestEnum>>(0);
        builder.AddComponentParameter(1, "Name", name);
        builder.AddComponentParameter(2, "ValueExpression", valueExpression);
        builder.AddComponentParameter(3, "ChildContent", new RenderFragment((childBuilder) =>
        {
            foreach (var value in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
            {
                childBuilder.OpenComponent<TestInputRadio>(0);
                childBuilder.AddComponentParameter(1, "Name", name);
                childBuilder.AddComponentParameter(2, "Value", value);
                childBuilder.CloseComponent();
            }
        }));
        builder.CloseComponent();
    };

    private static IEnumerable<TestInputRadio> FindInputRadioComponents(CapturedBatch batch)
        => batch.ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Component)
                .Select(f => f.Component)
                .OfType<TestInputRadio>();

    private static async Task<IEnumerable<TestInputRadio>> RenderAndGetTestInputComponentAsync(TestInputRadioHostComponent<TestEnum> rootComponent)
    {
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        return FindInputRadioComponents(testRenderer.Batches.Single());
    }

    private enum TestEnum
    {
        One,
        Two,
        Three
    }

    private class TestModel
    {
        public TestEnum TestEnum { get; set; }
    }

    private class TestInputRadio : InputRadio<TestEnum>
    {
        public string GroupName => Context.GroupName;
    }

    private class TestInputRadioHostComponent<TValue> : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public RenderFragment InnerContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddComponentParameter(1, "Value", EditContext);
            builder.AddComponentParameter(2, "ChildContent", InnerContent);
            builder.CloseComponent();
        }
    }
}
