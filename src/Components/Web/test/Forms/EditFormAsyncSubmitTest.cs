// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms;

public class EditFormAsyncSubmitTest
{
    private readonly TestRenderer _testRenderer;

    public EditFormAsyncSubmitTest()
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
    public async Task Submit_AwaitsAsyncValidationBeforeOnValidSubmit()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        TestAsyncValidator validator = null;
        var validSubmitCount = 0;
        var rootComponent = new TestEditFormHostComponent
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
        await RenderRootAsync(rootComponent);

        var dispatchTask = _testRenderer.DispatchEventAsync(GetSubmitEventHandlerId(), EventArgs.Empty);
        await WaitUntilAsync(() => validator.FormValidationStartCount == 1);

        Assert.Equal(0, validSubmitCount);

        validator.OpenGate(field, ValidationOutcome.Valid);
        await dispatchTask.WaitAsync(DefaultTimeout);

        Assert.Equal(1, validSubmitCount);
    }

    [Fact]
    public async Task Submit_InvalidAsyncValidation_FiresOnInvalidSubmit()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var validSubmitCount = 0;
        var invalidSubmitCount = 0;
        var rootComponent = new TestEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current => current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Invalid, ErrorMessage = "Invalid" }),
            OnValidSubmit = _ => validSubmitCount++,
            OnInvalidSubmit = _ => invalidSubmitCount++,
        };
        await RenderRootAsync(rootComponent);

        await _testRenderer.DispatchEventAsync(GetSubmitEventHandlerId(), EventArgs.Empty).WaitAsync(DefaultTimeout);

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
        var rootComponent = new TestEditFormHostComponent
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
        await RenderRootAsync(rootComponent);

        await _testRenderer.DispatchEventAsync(GetSubmitEventHandlerId(), EventArgs.Empty).WaitAsync(DefaultTimeout);

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
        var rootComponent = new TestEditFormHostComponent
        {
            EditContext = editContext,
            Configure = current => current.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid }),
            Created = current => validator = current,
            OnValidSubmit = _ => validSubmitCount++,
        };
        await RenderRootAsync(rootComponent);
        var pendingCts = new CancellationTokenSource();
        var pendingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var pendingRegistration = pendingCts.Token.Register(() => pendingTcs.TrySetCanceled(pendingCts.Token));
        editContext.AddValidationTask(field, pendingTcs.Task, pendingCts);

        await _testRenderer.DispatchEventAsync(GetSubmitEventHandlerId(), EventArgs.Empty).WaitAsync(DefaultTimeout);

        Assert.True(pendingCts.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
        Assert.Equal(1, validator.FormValidationStartCount);
        Assert.Equal(1, validSubmitCount);
    }

    private async Task RenderRootAsync(TestEditFormHostComponent rootComponent)
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
            if (DateTime.UtcNow - start > DefaultTimeout)
            {
                throw new TimeoutException("The expected condition was not reached before the timeout.");
            }

            await Task.Yield();
        }
    }

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private sealed class TestModel
    {
        public string StringProperty { get; set; }
    }

    private sealed class TestEditFormHostComponent : AutoRenderComponent
    {
        public EditContext EditContext { get; set; }

        public Action<TestAsyncValidator> Configure { get; set; }

        public Action<TestAsyncValidator> Created { get; set; }

        public Action<EditContext> OnValidSubmit { get; set; }

        public Action<EditContext> OnInvalidSubmit { get; set; }

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
            builder.AddComponentParameter(4, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(context => childBuilder =>
            {
                childBuilder.OpenComponent<TestAsyncValidatorComponent>(0);
                childBuilder.AddComponentParameter(1, nameof(TestAsyncValidatorComponent.Configure), Configure);
                childBuilder.AddComponentParameter(2, nameof(TestAsyncValidatorComponent.Created), EventCallback.Factory.Create<TestAsyncValidator>(this, Created));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class TestFormValueModelBinder : IFormValueMapper
    {
        public bool CanMap(Type valueType, string mappingScopeName, string formName) => false;

        public void Map(FormValueMappingContext context)
        {
        }
    }
}
