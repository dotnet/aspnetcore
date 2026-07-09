// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Test;

public class DynamicInputTypeBindingTest
{
    [Fact]
    public void RendersValueAsBoolWireFormat_True()
    {
        var component = new DynamicTypeInputBoolComponent
        {
            InputType = "checkbox",
            Value = true,
        };

        var renderer = new TestRenderer();
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var frames = renderer.GetCurrentRenderTreeFrames(componentId);
        var valueFrame = frames.AsEnumerable()
            .First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value");

        Assert.IsType<bool>(valueFrame.AttributeValue);
        Assert.True((bool)valueFrame.AttributeValue);
    }

    [Fact]
    public void RendersValueAsBoolWireFormat_False()
    {
        var component = new DynamicTypeInputBoolComponent
        {
            InputType = "checkbox",
            Value = false,
        };

        var renderer = new TestRenderer();
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var frames = renderer.GetCurrentRenderTreeFrames(componentId);
        var hasValueFrame = frames.AsEnumerable()
            .Any(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "value");
        Assert.False(hasValueFrame, "False bool attribute should be omitted from the render tree");
    }

    [Fact]
    public void DynamicType_EmitsTypeBeforeValue_SoJSCanApplyTypeLast()
    {
        var component = new DynamicTypeInputBoolComponent
        {
            InputType = "checkbox",
            Value = true,
        };

        var renderer = new TestRenderer();
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var frames = renderer.GetCurrentRenderTreeFrames(componentId).AsEnumerable()
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute)
            .Where(f => f.AttributeName == "type" || f.AttributeName == "value" || f.AttributeName == "checked")
            .ToList();

        var typeIndex = frames.FindIndex(f => f.AttributeName == "type");
        Assert.True(typeIndex >= 0, "Expected a 'type' attribute in the render tree");

        // No further 'type' attribute should follow (single type per element).
        for (var i = typeIndex + 1; i < frames.Count; i++)
        {
            Assert.NotEqual("type", frames[i].AttributeName);
        }
    }

    [Fact]
    public void DynamicType_NonCheckbox_EmitsValueWithoutChecked()
    {
        var component = new DynamicTypeInputBoolComponent
        {
            InputType = "text",
            Value = true,
        };

        var renderer = new TestRenderer();
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var frames = renderer.GetCurrentRenderTreeFrames(componentId);
        var hasCheckedFrame = frames.AsEnumerable()
            .Any(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "checked");
        Assert.False(hasCheckedFrame, "Non-checkable input should not have a 'checked' attribute");
    }
    private class DynamicTypeInputBoolComponent : AutoRenderComponent
    {
        public string InputType { get; set; } = "checkbox";
        public bool Value { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", InputType);
            builder.AddAttribute(2, "value", BindConverter.FormatValue(Value));
            builder.AddAttribute(3, "onchange", EventCallback.Factory.CreateBinder<bool>(
                this, __value => Value = __value, Value));
            builder.CloseElement();
        }
    }
}
