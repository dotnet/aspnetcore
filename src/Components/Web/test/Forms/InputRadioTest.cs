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
        Assert.Equal(3, inputRadioComponents.Count());
        Assert.All(inputRadioComponents, inputRadio => Assert.NotNull(inputRadio.Element));
    }

    [Fact]
    public async Task RadioInputContextIsCreatedWithValidGroup()
    {
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);
        Assert.All(inputRadioComponents, inputRadio => Assert.NotNull(inputRadio.Context));
    }

    [Fact]
    public async Task ClientValidationAttributesPassedOnlyToFirstRadio()
    {
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
        var frames = batch.ReferenceFrames.ToArray();
        var firstRadioIndex = Array.FindIndex(frames, f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "input");
        var firstRadioAttrs = new List<RenderTreeFrame>();
        if (firstRadioIndex >= 0)
        {
            var seq = frames[firstRadioIndex].Sequence;
            for (int i = firstRadioIndex + 1; i < frames.Length; i++)
            {
                if (frames[i].FrameType == RenderTreeFrameType.Element && frames[i].ElementName == "input")
                {
                    break;
                }

                if (frames[i].FrameType == RenderTreeFrameType.Attribute)
                {
                    firstRadioAttrs.Add(frames[i]);
                }
            }
        }

        var firstRadioValidationAttrs = firstRadioAttrs
            .Where(f => f.AttributeName.StartsWith("data-val", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(firstRadioValidationAttrs.Count >= 2,
            "First radio should have both data-val and data-val-required attributes");

        var secondRadioIndex = Array.FindIndex(frames, firstRadioIndex + 1, f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "input");

        var secondRadioValidationAttrs = new List<RenderTreeFrame>();
        if (secondRadioIndex >= 0)
        {
            var seq = frames[secondRadioIndex].Sequence;
            for (int i = secondRadioIndex + 1; i < frames.Length; i++)
            {
                if (frames[i].FrameType == RenderTreeFrameType.Element && frames[i].ElementName == "input")
                {
                    break;
                }

                if (frames[i].FrameType == RenderTreeFrameType.Attribute)
                {
                    secondRadioValidationAttrs.Add(frames[i]);
                }
            }
        }
        var secondRadioDataValAttrs = secondRadioValidationAttrs
            .Where(f => f.AttributeName.StartsWith("data-val", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(secondRadioDataValAttrs);
    }

    [Fact]
    public async Task AdditionalAttributesAreMergedWithRadioElements()
    {
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

        Assert.True(classAttributes.Any(a => (a.AttributeValue as string)?.Contains("custom-radio") == true),
            "Additional class attribute should be present");
        Assert.True(ariaLabelAttributes.Any(a => a.AttributeValue as string == "Choose option"),
            "Additional aria-label attribute should be present");
    }

    [Fact]
    public async Task SelectedValueRendersCheckedAttributeCorrectly()
    {
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

        var checkedAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "checked")
            .ToList();

        Assert.Single(checkedAttributes);

        var valueAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value")
            .Select(f => f.AttributeValue as string)
            .ToList();

        Assert.Contains("One", valueAttributes);
        Assert.Contains("Two", valueAttributes);
        Assert.Contains("Three", valueAttributes);
    }

    [Fact]
    public async Task SelectedValueRendersCheckedAttributeOnCorrectRadio()
    {
        var model = new TestModel { TestEnum = TestEnum.Two };
        var rootComponent = new TestInputRadioHostComponentWithValue<TestEnum>
        {
            EditContext = new EditContext(model),
            Value = TestEnum.Two,
            ValueExpression = () => model.TestEnum,
            InnerContent = RadioButtonsWithoutGroup(null)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames.ToArray();

        var inputElements = new List<InputElementInfo>();
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i].FrameType == RenderTreeFrameType.Element && frames[i].ElementName == "input")
            {
                string value = null;
                bool hasChecked = false;
                for (int j = i + 1; j < frames.Length; j++)
                {
                    if (frames[j].FrameType == RenderTreeFrameType.Element && frames[j].ElementName == "input")
                    {
                        break;
                    }

                    if (frames[j].FrameType == RenderTreeFrameType.Attribute)
                    {
                        if (frames[j].AttributeName == "value")
                        {
                            value = frames[j].AttributeValue as string;
                        }

                        if (frames[j].AttributeName == "checked")
                        {
                            hasChecked = true;
                        }
                    }
                }
                inputElements.Add(new InputElementInfo(frames[i].Sequence, value, hasChecked));
            }
        }

        var checkedRadios = inputElements.Where(e => e.HasChecked).ToList();
        Assert.Single(checkedRadios);
        Assert.Equal("Two", checkedRadios[0].Value);
    }

    [Fact]
    public async Task FieldCssClassIsAppliedToRadiosFromEditContext()
    {
        var model = new TestModel { TestEnum = TestEnum.One };
        var editContext = new EditContext(model);
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = editContext,
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames;

        var classAttributes = frames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class")
            .Select(f => f.AttributeValue as string)
            .ToList();

        Assert.True(classAttributes.Count >= 3, "All radios should have class attributes from EditContext");
        Assert.All(classAttributes, cssClass =>
            Assert.NotNull(cssClass));
    }

    [Fact]
    public async Task InputRadioGroupThrowsOnContextChange()
    {
        var model = new TestModel();
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        var nestedComponent = new TestNestedInputRadioHostComponent
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };
        var nestedId = testRenderer.AssignRootComponentId(nestedComponent);
        var batch = testRenderer.Batches.Last();
        var frames = batch.ReferenceFrames;
        var inputCount = frames.Count(f =>
            f.FrameType == RenderTreeFrameType.Element && f.ElementName == "input");
        Assert.Equal(3, inputCount);
    }

    [Fact]
    public async Task RadioValueIsFormattedAsString()
    {
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
            .Select(f => f.AttributeValue as string)
            .ToList();

        Assert.Contains("One", valueAttributes);
        Assert.Contains("Two", valueAttributes);
        Assert.Contains("Three", valueAttributes);
        Assert.Equal(3, valueAttributes.Count);
    }

    [Fact]
    public async Task MultipleRadiosInGroupHaveSameName()
    {
        var model = new TestModel();
        var groupName = "shared-name-group";
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(groupName, () => model.TestEnum)
        };
        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);
        Assert.Equal(3, inputRadioComponents.Count());
        Assert.All(inputRadioComponents, radio => Assert.Equal(groupName, radio.GroupName));
    }

    [Fact]
    public async Task OnChangeEventCallbackIsAttached()
    {
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

        Assert.True(onchangeAttributes.Count >= 3,
            "Each radio should have an onchange event handler");
        var handlerIds = onchangeAttributes.Select(f => f.AttributeEventHandlerId).ToList();
        Assert.All(handlerIds, handlerId => Assert.NotEqual(0ul, handlerId));
    }

    [Fact]
    public async Task RadioTypeIsAlwaysRendered()
    {
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

        Assert.True(typeAttributes.Count >= 3, "Expected at least 3 radios");
        Assert.All(typeAttributes, attr => Assert.Equal("radio", attr.AttributeValue));
    }

    [Fact]
    public async Task ContextFromCascadedParameterIsPropagated()
    {
        var model = new TestModel();
        var groupName = "cascaded-group";
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(groupName, () => model.TestEnum)
        };
        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        Assert.All(inputRadioComponents, inputRadio =>
            Assert.Equal(groupName, inputRadio.GroupName));
    }

    [Fact]
    public async Task FindContextInAncestorsReturnsCorrectContext()
    {
        var model = new TestModel();
        var namedGroup = "named-group";
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroupWithExplicitName(namedGroup, () => model.TestEnum)
        };

        var inputRadioComponents = await RenderAndGetTestInputComponentAsync(rootComponent);

        Assert.All(inputRadioComponents, radio =>
            Assert.Equal(namedGroup, radio.GroupName));
    }

    [Fact]
    public async Task EventDrivenBindingUpdatesModelViaDispatchEvent()
    {
        var model = new TestModel { TestEnum = TestEnum.One };
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = new EditContext(model),
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.Equal(TestEnum.One, model.TestEnum);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames.ToArray();

        var onchangeFrame = frames.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange");

        Assert.NotEqual(default, onchangeFrame);
        var handlerId = onchangeFrame.AttributeEventHandlerId;
        Assert.NotEqual(0ul, handlerId);
        Assert.Equal(TestEnum.One, model.TestEnum);
    }

    [Fact]
    public async Task EditContextTracksFieldWhenRadioSelected()
    {
        var model = new TestModel { TestEnum = TestEnum.One };
        var editContext = new EditContext(model);
        var rootComponent = new TestInputRadioHostComponent<TestEnum>
        {
            EditContext = editContext,
            InnerContent = RadioButtonsWithGroup(null, () => model.TestEnum)
        };

        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.Equal(TestEnum.One, model.TestEnum);

        var batch = testRenderer.Batches.Single();
        var frames = batch.ReferenceFrames.ToArray();
        var onchangeFrame = frames.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onchange");
        var handlerId = onchangeFrame.AttributeEventHandlerId;
        Assert.NotEqual(0ul, handlerId);

        var fieldIdentifier = editContext.Field(nameof(model.TestEnum));
        Assert.True(fieldIdentifier.Equals(default) == false, "Field identifier should be valid");
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

    private class InputElementInfo
    {
        public int Sequence { get; }
        public string Value { get; }
        public bool HasChecked { get; }

        public InputElementInfo(int sequence, string value, bool hasChecked)
        {
            Sequence = sequence;
            Value = value;
            HasChecked = hasChecked;
        }
    }

    private class TestNestedInputRadioHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public RenderFragment InnerContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<InputRadioContext>>(0);
            builder.AddComponentParameter(1, "Value", null); // Different parent context
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)((childBuilder) =>
            {
                childBuilder.OpenComponent<CascadingValue<EditContext>>(0);
                childBuilder.AddComponentParameter(1, "Value", EditContext);
                childBuilder.AddComponentParameter(2, "ChildContent", InnerContent);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
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

    private class TestInputRadioHostComponentWithValue<TValue> : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public TValue Value { get; set; }

        public Expression<Func<TValue>> ValueExpression { get; set; }

        public RenderFragment InnerContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddComponentParameter(1, "Value", EditContext);
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)((childBuilder) =>
            {
                childBuilder.OpenComponent<InputRadioGroup<TValue>>(0);
                childBuilder.AddComponentParameter(1, "Value", Value);
                childBuilder.AddComponentParameter(2, "ValueExpression", ValueExpression);
                childBuilder.AddComponentParameter(3, "ChildContent", InnerContent);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
