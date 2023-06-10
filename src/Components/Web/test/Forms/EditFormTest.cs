// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms;

public class EditFormTest
{
    private TestRenderer _testRenderer = new();

    public EditFormTest()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<IFormValueSupplier, TestFormValueSupplier>();
        _testRenderer = new(services.BuildServiceProvider());
    }

    [Fact]
    public async Task ThrowsIfBothEditContextAndModelAreSupplied()
    {
        // Arrange
        var editForm = new EditForm
        {
            EditContext = new EditContext(new TestModel()),
            Model = new TestModel()
        };
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(editForm);

        // Act/Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => testRenderer.RenderRootComponentAsync(componentId));
        Assert.StartsWith($"{nameof(EditForm)} requires a {nameof(EditForm.Model)} parameter, or an {nameof(EditContext)} parameter, but not both.", ex.Message);
    }

    [Fact]
    public async Task ThrowsIfBothEditContextAndModelAreNull()
    {
        // Arrange
        var editForm = new EditForm();
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(editForm);

        // Act/Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => testRenderer.RenderRootComponentAsync(componentId));
        Assert.StartsWith($"{nameof(EditForm)} requires either a {nameof(EditForm.Model)} parameter, or an {nameof(EditContext)} parameter, please provide one of these.", ex.Message);
    }

    [Fact]
    public async Task ReturnsEditContextWhenModelParameterUsed()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model
        };
        var editFormComponent = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Act
        var returnedEditContext = editFormComponent.EditContext;

        // Assert
        Assert.NotNull(returnedEditContext);
        Assert.Same(model, returnedEditContext.Model);
    }

    [Fact]
    public async Task ReturnsEditContextWhenEditContextParameterUsed()
    {
        // Arrange
        var editContext = new EditContext(new TestModel());
        var rootComponent = new TestEditFormHostComponent
        {
            EditContext = editContext
        };
        var editFormComponent = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Act
        var returnedEditContext = editFormComponent.EditContext;

        // Assert
        Assert.Same(editContext, returnedEditContext);
    }

    [Fact]
    public async Task FormElementNameAndAction_SetToComponentName_WhenFormNameIsProvided()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            FormName = "my-form",
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var attributes = GetFormElementAttributeFrames().ToArray();

        // Assert
        AssertFrame.Attribute(attributes[0], "name", "my-form");
        AssertFrame.Attribute(attributes[1], "action", "path?query=value&handler=my-form");
    }

    [Fact]
    public async Task FormElementNameAndAction_SetToComponentName_WhenCombiningWithDefaultParentBindingContext()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            FormName = "my-form",
            BindingContext = new ModelBindingContext("", "", t => true)
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var attributes = GetFormElementAttributeFrames().ToArray();

        // Assert
        AssertFrame.Attribute(attributes[0], "name", "my-form");
        AssertFrame.Attribute(attributes[1], "action", "path?query=value&handler=my-form");
    }

    [Fact]
    public async Task FormElementNameAndAction_SetToCombinedIdentifier_WhenCombiningWithNamedParentBindingContext()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            FormName = "my-form",
            BindingContext = new ModelBindingContext("parent-context", "path?handler=parent-context", t => true )
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var attributes = GetFormElementAttributeFrames().ToArray();

        // Assert
        AssertFrame.Attribute(attributes[0], "name", "parent-context.my-form");
        AssertFrame.Attribute(attributes[1], "action", "path?query=value&handler=parent-context.my-form");
    }

    [Fact]
    public async Task FormElementNameAndAction_CanBeExplicitlyOverriden()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            FormName = "my-form",
            AdditionalFormAttributes = new Dictionary<string, object>() {
                ["name"] = "my-explicit-name",
                ["action"] = "/somewhere/else",
            },
            BindingContext = new ModelBindingContext("parent-context", "path?handler=parent-context", t => true)
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var attributes = GetFormElementAttributeFrames().ToArray();

        // Assert
        AssertFrame.Attribute(attributes[0], "name", "my-explicit-name");
        AssertFrame.Attribute(attributes[1], "action", "/somewhere/else");
    }

    [Fact]
    public async Task FormElementNameAndAction_NotSetOnDefaultBindingContext()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            BindingContext = new ModelBindingContext("", "", t => true),
            SubmitHandler = ctx => { }
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var attributes = GetFormElementAttributeFrames();

        // Assert
        var frame = Assert.Single(attributes);
        AssertFrame.Attribute(frame, "onsubmit");
    }

    [Fact]
    public async Task FormElementNameAndAction_NotSetWhenNoFormNameAndNoBindingContext()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            SubmitHandler = ctx => { }
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var attributes = GetFormElementAttributeFrames();

        // Assert
        var frame = Assert.Single(attributes);
        AssertFrame.Attribute(frame, "onsubmit");
    }

    [Fact]
    public async Task EventHandlerName_NotSetWhenNoBindingContextProvided()
    {
        // Arrange
        var tracker = TrackEventNames();

        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            SubmitHandler = ctx => { }
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Assert
        Assert.Null(tracker.EventName);
    }

    [Fact]
    public async Task EventHandlerName_SetToBindingIdOnDefaultHandler()
    {
        // Arrange
        var tracker = TrackEventNames();

        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            BindingContext = new ModelBindingContext("", "", t => true)
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Assert
        Assert.Equal("", tracker.EventName);
    }

    [Fact]
    public async Task EventHandlerName_SetToFormNameWhenFormNameIsProvided()
    {
        // Arrange
        var tracker = TrackEventNames();

        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            FormName = "my-form",
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Assert
        Assert.Equal("my-form", tracker.EventName);
    }

    [Fact]
    public async Task EventHandlerName_SetToFormNameWhenParentBindingContextIsDefault()
    {
        // Arrange
        var tracker = TrackEventNames();
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            FormName = "my-form",
            BindingContext = new ModelBindingContext("", "", t => true)
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Assert
        Assert.Equal("my-form", tracker.EventName);
    }

    [Fact]
    public async Task EventHandlerName_SetToCombinedNameWhenParentBindingContextIsNamed()
    {
        // Arrange
        var tracker = TrackEventNames();
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            FormName = "my-form",
            BindingContext = new ModelBindingContext("parent-context", "path?handler=parent-context", t => true)
        };

        // Act
        _ = await RenderAndGetTestEditFormComponentAsync(rootComponent);

        // Assert
        Assert.Equal("parent-context.my-form", tracker.EventName);
    }

    private static EditForm FindEditFormComponent(CapturedBatch batch)
        => batch.ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Component)
                .Select(f => f.Component)
                .OfType<EditForm>()
                .Single();

    private async Task<EditForm> RenderAndGetTestEditFormComponentAsync(TestEditFormHostComponent hostComponent)
    {
        var componentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(componentId);
        return FindEditFormComponent(_testRenderer.Batches.Single());
    }

    private IEnumerable<RenderTreeFrame> GetFormElementAttributeFrames()
    {
        var frames = _testRenderer.Batches.Single().ReferenceFrames;
        var index = frames
            .Select((frame, index) => (frame, index))
            .Where(pair => pair.frame.FrameType == RenderTreeFrameType.Element)
            .Select(pair => pair.index)
            .Single();

        var attributes = frames
            .Skip(index + 1)
            .TakeWhile(f => f.FrameType == RenderTreeFrameType.Attribute);

        return attributes;
    }

    private int GetComponentFrameIndex()
    {
        var frames = _testRenderer.Batches.Single().ReferenceFrames;
        var frameIndex = frames
            .Select((frame, index) => (frame, index))
            .Where(pair => pair.frame.FrameType == RenderTreeFrameType.Component && pair.frame.Component is EditForm)
            .Select(pair => pair.index)
            .Single();
        return frameIndex;
    }

    private EventHandlerNameTracker TrackEventNames()
    {
        var tracker = new EventHandlerNameTracker();
        _testRenderer.TrackNamedEventHandlers = true;
        _testRenderer.OnNamedEvent += tracker.Track;
        return tracker;
    }

    private class EventHandlerNameTracker
    {
        public ulong EventHandlerId { get; private set; }

        public int ComponentId { get; private set; }

        public string EventName { get; private set; }

        internal void Track((ulong, int, string) tuple)
        {
            (EventHandlerId, ComponentId, EventName) = tuple;
        }
    }

    class TestModel
    {
        public string StringProperty { get; set; }
    }

    class TestEditFormHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public TestModel Model { get; set; }

        public ModelBindingContext BindingContext { get; set; }

        public Action<EditContext> SubmitHandler { get; set; }

        public string FormName { get; set; }

        public Dictionary<string, object> AdditionalFormAttributes { get; internal set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (BindingContext != null)
            {
                builder.OpenComponent<CascadingModelBinder>(0);
                builder.AddComponentParameter(1, nameof(CascadingModelBinder.Name), BindingContext.Name);
                builder.AddComponentParameter(3, nameof(CascadingModelBinder.ChildContent), (RenderFragment<ModelBindingContext>)((_) => RenderForm));
                builder.CloseComponent();
            }
            else
            {
                RenderForm(builder);
            }

            void RenderForm(RenderTreeBuilder builder)
            {
                builder.OpenComponent<EditForm>(0);
                // Order here is intentional to make sure that the test fails if we
                // accidentally capture a parameter in a named property.
                builder.AddMultipleAttributes(1, AdditionalFormAttributes);

                builder.AddComponentParameter(2, "Model", Model);
                builder.AddComponentParameter(3, "EditContext", EditContext);
                if (SubmitHandler != null)
                {
                    builder.AddComponentParameter(4, "OnValidSubmit", new EventCallback<EditContext>(null, SubmitHandler));
                }
                builder.AddComponentParameter(5, "FormHandlerName", FormName);

                builder.CloseComponent();
            }
        }
    }

    class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://localhost:85/subdir/", "https://localhost:85/subdir/path?query=value#hash");
        }
    }

    private class TestFormValueSupplier : IFormValueSupplier
    {
        public bool CanBind(string formName, Type valueType)
        {
            return false;
        }

        public bool CanConvertSingleValue(Type type)
        {
            return false;
        }

        public bool TryBind(string formName, Type valueType, [NotNullWhen(true)] out object boundValue)
        {
            boundValue = null;
            return false;
        }
    }
}
