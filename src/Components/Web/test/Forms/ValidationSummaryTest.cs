// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class ValidationSummaryTest
{
    private readonly TestRenderer _testRenderer = new();

    [Fact]
    public async Task RendersNothingWhenNoValidationErrors()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);

        // Act
        var summaryId = await RenderValidationSummaryAsync(editContext);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId);

        // Assert - no ul rendered when there are no errors
        Assert.DoesNotContain(frames.AsEnumerable(),
            f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "ul");
    }

    [Fact]
    public async Task RendersUlWithDefaultClassWhenErrorsExist()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Name is required");

        // Act
        var summaryId = await RenderValidationSummaryAsync(editContext);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId);

        // Assert
        Assert.Equal("validation-errors", GetClassAttribute(frames));
    }

    [Fact]
    public async Task MergesAdditionalClassAttributeWithDefaultClass()
    {
        // Regression test: previously the user's class= silently overwrote "validation-errors"
        // because AddAttribute(class) was called before AddMultipleAttributes, letting
        // the latter win. After the fix both classes must be present.
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Name is required");
        var additionalAttributes = new Dictionary<string, object> { ["class"] = "pt-2" };

        // Act
        var summaryId = await RenderValidationSummaryAsync(editContext, additionalAttributes: additionalAttributes);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId);

        // Assert - both classes present
        var cssClass = GetClassAttribute(frames);
        Assert.Contains("validation-errors", cssClass);
        Assert.Contains("pt-2", cssClass);
    }

    [Fact]
    public async Task DefaultClassIsNotLostWhenAdditionalClassIsProvided()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Name is required");
        var additionalAttributes = new Dictionary<string, object> { ["class"] = "custom-class" };

        // Act
        var summaryId = await RenderValidationSummaryAsync(editContext, additionalAttributes: additionalAttributes);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId);

        // Assert - "validation-errors" must survive alongside the user's class
        Assert.Contains("validation-errors", GetClassAttribute(frames));
    }

    [Fact]
    public async Task PassesThroughNonClassAdditionalAttributes()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Name is required");
        var additionalAttributes = new Dictionary<string, object>
        {
            ["data-test"] = "validation-errors",
            ["id"] = "error-summary",
        };

        // Act
        var summaryId = await RenderValidationSummaryAsync(editContext, additionalAttributes: additionalAttributes);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId);

        // Assert
        var attrs = frames.AsEnumerable()
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute)
            .ToDictionary(f => f.AttributeName, f => f.AttributeValue?.ToString());

        Assert.Equal("validation-errors", attrs["data-test"]);
        Assert.Equal("error-summary", attrs["id"]);
    }

    [Fact]
    public async Task RendersValidationMessagesAsLiItems()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Name is required");
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Name must be at least 3 characters");

        // Act
        var summaryId = await RenderValidationSummaryAsync(editContext);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId);

        // Assert
        var liElements = frames.AsEnumerable()
            .Where(f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "li")
            .ToList();
        Assert.Equal(2, liElements.Count);
    }

    [Fact]
    public async Task EachLiItemHasValidationMessageClass()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Error");

        // Act
        var summaryId = await RenderValidationSummaryAsync(editContext);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId).AsEnumerable().ToList();

        // Assert - find the li element and check the class attribute that follows it
        var liIndex = frames.FindIndex(f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "li");
        Assert.NotEqual(-1, liIndex);
        var liClassFrame = frames.Skip(liIndex + 1)
            .First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class");
        Assert.Equal("validation-message", liClassFrame.AttributeValue);
    }

    [Fact]
    public async Task ThrowsWhenUsedOutsideEditContext()
    {
        // Arrange
        var summary = new ValidationSummary();
        var componentId = _testRenderer.AssignRootComponentId(summary);

        // Act/Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _testRenderer.RenderRootComponentAsync(componentId));
        Assert.Contains(nameof(EditContext), ex.Message);
    }

    [Fact]
    public async Task ModelParameterShowsOnlyModelLevelErrors()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(new FieldIdentifier(model, string.Empty), "Model-level error");
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Field-level error (should be excluded)");

        // Act
        var summaryId = await RenderValidationSummaryAsync(editContext, model: model);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId);

        // Assert - only the model-level message is shown
        var liElements = frames.AsEnumerable()
            .Where(f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "li")
            .ToList();
        Assert.Single(liElements);
    }

    [Fact]
    public async Task ReRendersWhenValidationStateChanges()
    {
        // Arrange - start with no errors
        var model = new TestModel();
        var editContext = new EditContext(model);
        var summaryId = await RenderValidationSummaryAsync(editContext);

        Assert.DoesNotContain(
            _testRenderer.GetCurrentRenderTreeFrames(summaryId).AsEnumerable(),
            f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "ul");

        // Act - add errors and notify
        var messageStore = new ValidationMessageStore(editContext);
        messageStore.Add(editContext.Field(nameof(TestModel.Name)), "Name is required");
        editContext.NotifyValidationStateChanged();

        // Assert - ul now present
        var frames = _testRenderer.GetCurrentRenderTreeFrames(summaryId);
        Assert.Contains(frames.AsEnumerable(),
            f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "ul");
    }

    // --- Helpers ---

    private async Task<int> RenderValidationSummaryAsync(
        EditContext editContext,
        Dictionary<string, object>? additionalAttributes = null,
        object? model = null)
    {
        var hostComponent = new TestValidationSummaryHostComponent
        {
            EditContext = editContext,
            AdditionalAttributes = additionalAttributes,
            Model = model,
        };

        var hostId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostId);

        return _testRenderer.Batches.Single()
            .GetComponentFrames<ValidationSummary>()
            .Single()
            .ComponentId;
    }

    private static string? GetClassAttribute(ArrayRange<RenderTreeFrame> frames)
        => frames.AsEnumerable()
            .FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "class")
            .AttributeValue?.ToString();

    private class TestModel
    {
        public string Name { get; set; } = "";
    }

    private class TestValidationSummaryHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; } = default!;
        public Dictionary<string, object>? AdditionalAttributes { get; set; }
        public object? Model { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddComponentParameter(1, "Value", EditContext);
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<ValidationSummary>(0);
                childBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                if (Model is not null)
                {
                    childBuilder.AddComponentParameter(2, "Model", Model);
                }
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
