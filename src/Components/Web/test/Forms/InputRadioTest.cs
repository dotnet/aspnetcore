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
