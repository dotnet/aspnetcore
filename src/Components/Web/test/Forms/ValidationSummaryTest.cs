// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class ValidationSummaryTest
{
    [Fact]
    public async Task RendersValidationErrorsClass_WhenNoAdditionalAttributes()
    {
        var classAttribute = await RenderAndGetUlClassAttribute(additionalAttributes: null);

        Assert.Equal("validation-errors", classAttribute);
    }

    [Fact]
    public async Task CombinesCustomClassWithValidationErrors()
    {
        var classAttribute = await RenderAndGetUlClassAttribute(new Dictionary<string, object>
        {
            { "class", "pt-2" },
        });

        Assert.Equal("pt-2 validation-errors", classAttribute);
    }

    [Fact]
    public async Task SplatsAdditionalAttributesToUl()
    {
        var frames = await RenderAndGetValidationSummaryFrames(new Dictionary<string, object>
        {
            { "data-test", "validation-errors" },
        });

        var ulAttributes = GetUlAttributes(frames);
        Assert.Contains(ulAttributes, a => a.AttributeName == "data-test" && (string)a.AttributeValue == "validation-errors");
        Assert.Equal("validation-errors", GetClassValue(ulAttributes));
    }

    [Fact]
    public async Task DoesNotRenderUl_WhenNoValidationMessages()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var rootComponent = new TestValidationSummaryHostComponent
        {
            EditContext = editContext,
        };
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var frames = GetValidationSummaryFrames(testRenderer);
        Assert.DoesNotContain(frames.AsEnumerable(), f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "ul");
    }

    private static async Task<string> RenderAndGetUlClassAttribute(Dictionary<string, object> additionalAttributes)
    {
        var frames = await RenderAndGetValidationSummaryFrames(additionalAttributes);
        return GetClassValue(GetUlAttributes(frames));
    }

    private static async Task<ArrayRange<RenderTreeFrame>> RenderAndGetValidationSummaryFrames(Dictionary<string, object> additionalAttributes)
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(new FieldIdentifier(model, nameof(TestModel.StringProperty)), "An error");

        var rootComponent = new TestValidationSummaryHostComponent
        {
            EditContext = editContext,
            AdditionalAttributes = additionalAttributes,
        };
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        return GetValidationSummaryFrames(testRenderer);
    }

    private static ArrayRange<RenderTreeFrame> GetValidationSummaryFrames(TestRenderer testRenderer)
    {
        var batch = testRenderer.Batches.Single();
        var componentId = batch.GetComponentFrames<ValidationSummary>().Single().ComponentId;
        return testRenderer.GetCurrentRenderTreeFrames(componentId);
    }

    private static List<RenderTreeFrame> GetUlAttributes(ArrayRange<RenderTreeFrame> frames)
    {
        var result = new List<RenderTreeFrame>();
        var seenUl = false;
        foreach (var frame in frames.AsEnumerable())
        {
            if (!seenUl)
            {
                if (frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "ul")
                {
                    seenUl = true;
                }
                continue;
            }

            if (frame.FrameType != RenderTreeFrameType.Attribute)
            {
                break;
            }
            result.Add(frame);
        }
        return result;
    }

    private static string GetClassValue(List<RenderTreeFrame> ulAttributes)
        => ulAttributes.Single(a => a.AttributeName == "class").AttributeValue as string;

    private class TestModel
    {
        public string StringProperty { get; set; }
    }

    private class TestValidationSummaryHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public Dictionary<string, object> AdditionalAttributes { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddComponentParameter(1, "Value", EditContext);
            builder.AddComponentParameter(2, "ChildContent", new RenderFragment(childBuilder =>
            {
                childBuilder.OpenComponent<ValidationSummary>(0);
                childBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
