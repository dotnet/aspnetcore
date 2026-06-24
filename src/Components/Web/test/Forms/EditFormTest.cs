// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Infrastructure;
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
        services.AddSingleton<IFormValueMapper, TestFormValueModelBinder>();
        services.AddAntiforgery();
        services.AddLogging();
        services.AddSingleton<ComponentStatePersistenceManager>();
        services.AddSingleton(services => services.GetRequiredService<ComponentStatePersistenceManager>().State);
        services.AddSingleton<AntiforgeryStateProvider, DefaultAntiforgeryStateProvider>();
        _testRenderer = new(services.BuildServiceProvider());
    }

    [Fact]
    public async Task SubmitAsync_AwaitsAsyncValidationBeforeOnValidSubmit()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        TestAsyncValidator validator = null;
        var validSubmitCount = 0;
        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current =>
            {
                current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid });
                current.GetGate(field);
            },
            Created = current => validator = current,
            OnValidSubmit = _ => validSubmitCount++,
        };
        await RenderAsyncRootAsync(rootComponent);

        var editForm = FindEditFormComponent(_testRenderer.Batches.Last());
        var submitTask = editForm.SubmitAsync();
        await WaitUntilAsync(() => validator.FormValidationStartCount == 1);

        Assert.Equal(0, validSubmitCount);

        validator.OpenGate(field, ValidationOutcome.Valid);
        await submitTask.WaitAsync(DefaultAsyncTimeout);

        Assert.Equal(1, validSubmitCount);
    }

    [Fact]
    public async Task SubmitAsync_WithOnSubmit_InvokesHandlerAndWaitsForAsyncValidation()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));

        var submitCount = 0;

        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,

            Configure = current =>
            {
                current.Configure(field, new ValidationConfig
                {
                    Outcome = ValidationOutcome.Valid
                });

                current.GetGate(field);
            },

            // ✅ OnSubmit handler
            OnSubmit = _ =>
            {
                submitCount++;
            }
        };

        await RenderAsyncRootAsync(rootComponent);

        var editForm = FindEditFormComponent(_testRenderer.Batches.Last());
        var submitTask = editForm.SubmitAsync();

        await submitTask.WaitAsync(DefaultAsyncTimeout);

        Assert.Equal(1, submitCount);
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
    public async Task SubmitAsync_InvalidAsyncValidation_FiresOnInvalidSubmit()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var validSubmitCount = 0;
        var invalidSubmitCount = 0;
        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current => current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Invalid, ErrorMessage = "Invalid" }),
            OnValidSubmit = _ => validSubmitCount++,
            OnInvalidSubmit = _ => invalidSubmitCount++,
        };
        await RenderAsyncRootAsync(rootComponent);

        var editForm = FindEditFormComponent(_testRenderer.Batches.Last());
        await editForm.SubmitAsync().WaitAsync(DefaultAsyncTimeout);

        Assert.Equal(0, validSubmitCount);
        Assert.Equal(1, invalidSubmitCount);
        Assert.Equal(new[] { "Invalid" }, editContext.GetValidationMessages(field));
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
    public async Task SubmitAsync_AsyncValidatorThrows_FiresOnInvalidSubmitWithFaultedContext()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var validSubmitCount = 0;
        var invalidSubmitCount = 0;
        var observedFaulted = false;
        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current => current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.ThrowInfraException }),
            OnValidSubmit = _ => validSubmitCount++,
            OnInvalidSubmit = context =>
            {
                invalidSubmitCount++;
                observedFaulted = context.IsValidationFaulted();
            },
        };
        await RenderAsyncRootAsync(rootComponent);

        var editForm = FindEditFormComponent(_testRenderer.Batches.Last());
        await editForm.SubmitAsync().WaitAsync(DefaultAsyncTimeout);

        Assert.Equal(0, validSubmitCount);
        Assert.Equal(1, invalidSubmitCount);
        Assert.True(observedFaulted);
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
    public async Task SubmitAsync_WithPendingFieldTask_CancelsFieldTaskAndRunsFormValidation()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        TestAsyncValidator validator = null;
        var validSubmitCount = 0;
        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current => current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid }),
            Created = current => validator = current,
            OnValidSubmit = _ => validSubmitCount++,
        };
        await RenderAsyncRootAsync(rootComponent);
        var pendingCts = new CancellationTokenSource();
        var pendingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var pendingRegistration = pendingCts.Token.Register(() => pendingTcs.TrySetCanceled(pendingCts.Token));
        editContext.AddValidationTask(field, pendingTcs.Task, pendingCts);

        var editForm = FindEditFormComponent(_testRenderer.Batches.Last());
        await editForm.SubmitAsync().WaitAsync(DefaultAsyncTimeout);

        Assert.True(pendingCts.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
        Assert.Equal(1, validator.FormValidationStartCount);
        Assert.Equal(1, validSubmitCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReturnsEditContextWhenEditContextParameterUsed(bool createFieldPath)
    {
        // Arrange
        var editContext = new EditContext(new TestModel()) { ShouldUseFieldIdentifiers = createFieldPath };
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
    public async Task DoesNotAddSSRContentWhenNoMappingContextPresent()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestEditFormHostComponent
        {
            Model = model,
            FormName = "my-form",
        };

        // Act
        await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var editFormComponentId = _testRenderer.Batches.Single()
            .GetComponentFrames<EditForm>().Single().ComponentId;
        var editFormFrames = _testRenderer.GetCurrentRenderTreeFrames(editFormComponentId);

        // Assert:
        //  - Does not set any "method" attribute
        //  - Does not assign any name to the submit event
        Assert.Collection(editFormFrames.AsEnumerable(),
            frame => AssertFrame.Region(frame, 7),
            frame => AssertFrame.Element(frame, "form", 6),
            frame => AssertFrame.Attribute(frame, "onsubmit"),
            frame => AssertFrame.Component<CascadingValue<EditContext>>(frame, 4),
            frame => AssertFrame.Attribute(frame, "IsFixed", true),
            frame => AssertFrame.Attribute(frame, "Value"),
            frame => AssertFrame.Attribute(frame, "ChildContent"));
    }

    [Fact]
    public async Task AddSSRContentWhenMappingContextPresent()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var rootComponent = new TestEditFormHostComponent
        {
            FormName = "my-form",
            MappingContextName = "mapping-context-name",
            EditContext = editContext,
        };

        // Act
        await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var editFormComponentId = _testRenderer.Batches.Single()
            .GetComponentFrames<EditForm>().Single().ComponentId;
        var editFormFrames = _testRenderer.GetCurrentRenderTreeFrames(editFormComponentId);

        // Assert
        Assert.Collection(editFormFrames.AsEnumerable(),
            frame => AssertFrame.Region(frame, 13),
            frame => AssertFrame.Element(frame, "form", 12),

            // Sets "method" to "post" by default
            frame => AssertFrame.Attribute(frame, "method", "post"),

            // Assigns name to the submit event
            frame => AssertFrame.Attribute(frame, "onsubmit"),
            frame => AssertFrame.NamedEvent(frame, "onsubmit", "my-form"),

            frame => AssertFrame.Region(frame, 4),

            // Adds FormMappingValidator child
            frame => AssertFrame.Component<FormMappingValidator>(frame, 2),
            frame => AssertFrame.Attribute(frame, nameof(FormMappingValidator.CurrentEditContext), editContext),

            // Adds AntiforgeryToken child
            frame => AssertFrame.Component<AntiforgeryToken>(frame, 1),

            frame => AssertFrame.Component<CascadingValue<EditContext>>(frame, 4),
            frame => AssertFrame.Attribute(frame, "IsFixed", true),
            frame => AssertFrame.Attribute(frame, "Value"),
            frame => AssertFrame.Attribute(frame, "ChildContent"));
    }

    [Fact]
    public async Task CanOverrideMethodWhenMappingContextPresent()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var rootComponent = new TestEditFormHostComponent
        {
            FormName = "my-form",
            MappingContextName = "mapping-context-name",
            EditContext = editContext,
            AdditionalFormAttributes = new Dictionary<string, object>
            {
                { "method", "my method" },
                { "custom attribute", "some value" },
            },
        };

        // Act
        await RenderAndGetTestEditFormComponentAsync(rootComponent);
        var editFormComponentId = _testRenderer.Batches.Single()
            .GetComponentFrames<EditForm>().Single().ComponentId;
        var editFormFrames = _testRenderer.GetCurrentRenderTreeFrames(editFormComponentId);
        var editFormAttributes = editFormFrames.AsEnumerable()
            .SkipWhile(f => f.FrameType != RenderTreeFrameType.Attribute)
            .TakeWhile(f => f.FrameType == RenderTreeFrameType.Attribute)
            .ToDictionary(f => f.AttributeName, f => f.AttributeValue);

        // Assert
        Assert.Equal("my method", editFormAttributes["method"]);
        Assert.Equal("some value", editFormAttributes["custom attribute"]);
    }

    [Fact]
    public async Task Submit_AwaitsAsyncValidationBeforeOnValidSubmit()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        TestAsyncValidator validator = null;
        var validSubmitCount = 0;
        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current =>
            {
                current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid });
                current.GetGate(field);
            },
            Created = current => validator = current,
            OnValidSubmit = _ => validSubmitCount++,
        };
        await RenderAsyncRootAsync(rootComponent);

        var dispatchTask = _testRenderer.DispatchEventAsync(GetSubmitEventHandlerId(), EventArgs.Empty);
        await WaitUntilAsync(() => validator.FormValidationStartCount == 1);

        Assert.Equal(0, validSubmitCount);

        validator.OpenGate(field, ValidationOutcome.Valid);
        await dispatchTask.WaitAsync(DefaultAsyncTimeout);

        Assert.Equal(1, validSubmitCount);
    }

    [Fact]
    public async Task Submit_InvalidAsyncValidation_FiresOnInvalidSubmit()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var validSubmitCount = 0;
        var invalidSubmitCount = 0;
        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current => current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Invalid, ErrorMessage = "Invalid" }),
            OnValidSubmit = _ => validSubmitCount++,
            OnInvalidSubmit = _ => invalidSubmitCount++,
        };
        await RenderAsyncRootAsync(rootComponent);

        await _testRenderer.DispatchEventAsync(GetSubmitEventHandlerId(), EventArgs.Empty).WaitAsync(DefaultAsyncTimeout);

        Assert.Equal(0, validSubmitCount);
        Assert.Equal(1, invalidSubmitCount);
        Assert.Equal(new[] { "Invalid" }, editContext.GetValidationMessages(field));
    }

    [Fact]
    public async Task Submit_AsyncValidatorThrows_FiresOnInvalidSubmitWithFaultedContext()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var validSubmitCount = 0;
        var invalidSubmitCount = 0;
        var observedFaulted = false;
        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current => current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.ThrowInfraException }),
            OnValidSubmit = _ => validSubmitCount++,
            OnInvalidSubmit = context =>
            {
                invalidSubmitCount++;
                observedFaulted = context.IsValidationFaulted();
            },
        };
        await RenderAsyncRootAsync(rootComponent);

        await _testRenderer.DispatchEventAsync(GetSubmitEventHandlerId(), EventArgs.Empty).WaitAsync(DefaultAsyncTimeout);

        Assert.Equal(0, validSubmitCount);
        Assert.Equal(1, invalidSubmitCount);
        Assert.True(observedFaulted);
    }

    [Fact]
    public async Task Submit_WithPendingFieldTask_CancelsFieldTaskAndRunsFormValidation()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        TestAsyncValidator validator = null;
        var validSubmitCount = 0;
        var rootComponent = new AsyncEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current => current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid }),
            Created = current => validator = current,
            OnValidSubmit = _ => validSubmitCount++,
        };
        await RenderAsyncRootAsync(rootComponent);
        var pendingCts = new CancellationTokenSource();
        var pendingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var pendingRegistration = pendingCts.Token.Register(() => pendingTcs.TrySetCanceled(pendingCts.Token));
        editContext.AddValidationTask(field, pendingTcs.Task, pendingCts);

        await _testRenderer.DispatchEventAsync(GetSubmitEventHandlerId(), EventArgs.Empty).WaitAsync(DefaultAsyncTimeout);

        Assert.True(pendingCts.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
        Assert.Equal(1, validator.FormValidationStartCount);
        Assert.Equal(1, validSubmitCount);
    }

    private async Task RenderAsyncRootAsync(AsyncEditFormHostComponent rootComponent)
    {
        var componentId = _testRenderer.AssignRootComponentId(rootComponent);
        await _testRenderer.RenderRootComponentAsync(componentId);
    }

    private ulong GetSubmitEventHandlerId()
    {
        var editFormComponentId = _testRenderer.Batches.Last().ReferenceFrames.AsEnumerable()
            .Where(frame => frame.FrameType == RenderTreeFrameType.Component)
            .Where(frame => frame.Component is EditForm)
            .Select(frame => frame.ComponentId)
            .Single();
        var editFormFrames = _testRenderer.GetCurrentRenderTreeFrames(editFormComponentId);
        return editFormFrames.AsEnumerable()
            .Where(frame => frame.FrameType == RenderTreeFrameType.Attribute)
            .Where(frame => frame.AttributeName == "onsubmit")
            .Select(frame => frame.AttributeEventHandlerId)
            .Single();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > DefaultAsyncTimeout)
            {
                throw new TimeoutException("The expected condition was not reached before the timeout.");
            }

            await Task.Yield();
        }
    }

    private static readonly TimeSpan DefaultAsyncTimeout = TimeSpan.FromSeconds(5);

    private sealed class AsyncEditFormHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public Action<TestAsyncValidator> Configure { get; set; }

        public Action<TestAsyncValidator> Created { get; set; }

        public Action<EditContext> OnValidSubmit { get; set; }

        public Action<EditContext> OnInvalidSubmit { get; set; }

         public Action<EditContext> OnSubmit { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<EditForm>(0);
            builder.AddComponentParameter(1, nameof(EditForm.EditContext), EditContext);
            if (OnValidSubmit is not null)
            {
                builder.AddComponentParameter(2, nameof(EditForm.OnValidSubmit), EventCallback.Factory.Create(this, OnValidSubmit));
            }
            if (OnInvalidSubmit is not null)
            {
                builder.AddComponentParameter(3, nameof(EditForm.OnInvalidSubmit), EventCallback.Factory.Create(this, OnInvalidSubmit));
            }
            if (OnSubmit is not null)
            {
                builder.AddComponentParameter(4, nameof(EditForm.OnSubmit), EventCallback.Factory.Create(this, OnSubmit));
            }
            builder.AddComponentParameter(5, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(context => childBuilder =>
            {
                childBuilder.OpenComponent<TestAsyncValidatorComponent>(0);
                childBuilder.AddComponentParameter(1, nameof(TestAsyncValidatorComponent.Configure), Configure);
                childBuilder.AddComponentParameter(2, nameof(TestAsyncValidatorComponent.Created), EventCallback.Factory.Create<TestAsyncValidator>(this, Created));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
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

    class TestModel
    {
        public string StringProperty { get; set; }
    }

    class TestEditFormHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public TestModel Model { get; set; }

        public string MappingContextName { get; set; }

        public Action<EditContext> SubmitHandler { get; set; }

        public string FormName { get; set; }

        public Dictionary<string, object> AdditionalFormAttributes { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (MappingContextName is not null)
            {
                builder.OpenComponent<FormMappingScope>(0);
                builder.AddComponentParameter(1, nameof(FormMappingScope.Name), MappingContextName);
                builder.AddComponentParameter(3, nameof(FormMappingScope.ChildContent), (RenderFragment<FormMappingContext>)(_ => RenderForm));
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
                builder.AddComponentParameter(5, "FormName", FormName);

                builder.CloseComponent();
            }
        }
    }

    private class TestFormValueModelBinder : IFormValueMapper
    {
        public bool CanMap(Type valueType, string mappingScopeName, string formName) => false;
        public void Map(FormValueMappingContext context) { }
    }
}
