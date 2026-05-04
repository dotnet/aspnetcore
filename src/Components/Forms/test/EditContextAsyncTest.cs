// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class EditContextAsyncTest
{
    [Fact]
    public async Task FieldValidation_Valid_CompletesWithoutMessages()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid });
        validator.GetGate(field);
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.NotifyFieldChanged(field);

        Assert.True(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationPending());
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Equal(1, notificationCount);

        validator.OpenGate(field, ValidationOutcome.Valid);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Empty(editContext.GetValidationMessages(field));
        Assert.True(editContext.IsValid(field));
    }

    [Fact]
    public async Task FieldValidation_Invalid_AddsMessage()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Invalid, ErrorMessage = "Invalid value" });
        validator.GetGate(field);

        editContext.NotifyFieldChanged(field);
        validator.OpenGate(field, ValidationOutcome.Invalid);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Equal(new[] { "Invalid value" }, editContext.GetValidationMessages(field));
        Assert.False(editContext.IsValid(field));
    }

    [Fact]
    public async Task FieldValidation_InfrastructureException_MarksFieldFaulted()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.ThrowInfraException });
        validator.GetGate(field);
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.NotifyFieldChanged(field);
        validator.OpenGate(field, ValidationOutcome.ThrowInfraException);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.True(editContext.IsValidationFaulted(field));
        Assert.False(editContext.IsValidationFaulted());
        Assert.Empty(editContext.GetValidationMessages(field));
        Assert.True(notificationCount >= 2);
    }

    [Fact]
    public async Task FieldValidation_Reedit_CancelsPreviousTask()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        var firstGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tokens = new List<CancellationToken>();
        var callCount = 0;
        validator.Configure(field, new ValidationConfig
        {
            Custom = async (_, token) =>
            {
                tokens.Add(token);
                callCount++;
                if (callCount == 1)
                {
                    await firstGate.Task.WaitAsync(token);
                }
                else
                {
                    await secondGate.Task.WaitAsync(token);
                }

                return null;
            }
        });

        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => tokens.Count == 1);
        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => tokens.Count == 2);

        Assert.True(tokens[0].IsCancellationRequested);
        Assert.True(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        await WaitUntilAsync(() => validator.CancellationObservedCount(field) == 1);

        secondGate.SetResult();
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public async Task ValidateAsync_CancelsPendingFieldTask()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        var fieldGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var formGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken fieldToken = default;
        validator.Configure(field, new ValidationConfig
        {
            Custom = async (_, token) =>
            {
                if (!fieldToken.CanBeCanceled)
                {
                    fieldToken = token;
                    await fieldGate.Task.WaitAsync(token);
                }
                else
                {
                    await formGate.Task;
                }

                return null;
            }
        });
        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => fieldToken.CanBeCanceled);

        var validateTask = editContext.ValidateAsync();
        await WaitUntilAsync(() => editContext.IsValidationPending());

        Assert.True(fieldToken.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));

        formGate.SetResult();
        Assert.True(await validateTask.WaitAsync(DefaultTimeout));
    }

    [Fact]
    public async Task FieldValidation_MultipleFields_CompleteIndependently()
    {
        var editContext = new EditContext(new TestModel());
        var first = editContext.Field(nameof(TestModel.StringProperty));
        var second = editContext.Field(nameof(TestModel.OtherProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(first, new ValidationConfig { Outcome = ValidationOutcome.Valid });
        validator.Configure(second, new ValidationConfig { Outcome = ValidationOutcome.ThrowInfraException });
        validator.GetGate(first);
        validator.GetGate(second);

        editContext.NotifyFieldChanged(first);
        editContext.NotifyFieldChanged(second);

        Assert.True(editContext.IsValidationPending(first));
        Assert.True(editContext.IsValidationPending(second));

        validator.OpenGate(second, ValidationOutcome.ThrowInfraException);
        await WaitUntilAsync(() => !editContext.IsValidationPending(second));

        Assert.True(editContext.IsValidationPending(first));
        Assert.True(editContext.IsValidationFaulted(second));
        Assert.False(editContext.IsValidationFaulted(first));

        validator.OpenGate(first, ValidationOutcome.Valid);
        await WaitUntilAsync(() => !editContext.IsValidationPending(first));

        Assert.False(editContext.IsValidationFaulted(first));
    }

    [Fact]
    public async Task ValidateAsync_AllFieldsValid_ReturnsTrue()
    {
        var editContext = new EditContext(new TestModel());
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(editContext.Field(nameof(TestModel.StringProperty)), new ValidationConfig { Outcome = ValidationOutcome.Valid });
        validator.Configure(editContext.Field(nameof(TestModel.OtherProperty)), new ValidationConfig { Outcome = ValidationOutcome.Valid });

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.Empty(editContext.GetValidationMessages());
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_FieldInvalid_ReturnsFalseAndAddsMessage()
    {
        var editContext = new EditContext(new TestModel());
        var valid = editContext.Field(nameof(TestModel.StringProperty));
        var invalid = editContext.Field(nameof(TestModel.OtherProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(valid, new ValidationConfig { Outcome = ValidationOutcome.Valid });
        validator.Configure(invalid, new ValidationConfig { Outcome = ValidationOutcome.Invalid, ErrorMessage = "Other invalid" });

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.Empty(editContext.GetValidationMessages(valid));
        Assert.Equal(new[] { "Other invalid" }, editContext.GetValidationMessages(invalid));
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_FieldValidatorThrows_ReturnsFalseAndMarksFormFaulted()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.ThrowInfraException });

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public async Task ValidateAsync_AsyncHandlerThrowsSynchronously_IsContained()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => throw new InvalidOperationException("sync failure");

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_MultipleValidators_AwaitsBothAndCombinesMessages()
    {
        var editContext = new EditContext(new TestModel());
        var first = editContext.Field(nameof(TestModel.StringProperty));
        var second = editContext.Field(nameof(TestModel.OtherProperty));
        using var firstValidator = new TestAsyncValidator(editContext);
        using var secondValidator = new TestAsyncValidator(editContext);
        firstValidator.Configure(first, new ValidationConfig { Outcome = ValidationOutcome.Invalid, ErrorMessage = "First invalid" });
        secondValidator.Configure(second, new ValidationConfig { Outcome = ValidationOutcome.Invalid, ErrorMessage = "Second invalid" });

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.Equal(1, firstValidator.FormValidationStartCount);
        Assert.Equal(1, secondValidator.FormValidationStartCount);
        Assert.Equal(new[] { "First invalid", "Second invalid" }, editContext.GetValidationMessages().OrderBy(message => message));
    }

    [Fact]
    public async Task IsValidationPending_Overloads_SeparateFieldAndFormState()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid });
        validator.GetGate(field);

        editContext.NotifyFieldChanged(field);

        Assert.True(editContext.IsValidationPending(field));
        Assert.True(editContext.IsValidationPending(() => model.StringProperty));
        Assert.False(editContext.IsValidationPending());

        validator.OpenGate(field, ValidationOutcome.Valid);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));
    }

    [Fact]
    public async Task IsValidationFaulted_Overloads_SeparateFieldAndFormState()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.AddValidationTask(field, tcs.Task, new CancellationTokenSource());

        tcs.SetException(new InvalidOperationException("field failure"));
        await WaitUntilAsync(() => editContext.IsValidationFaulted(field));

        Assert.True(editContext.IsValidationFaulted(() => model.StringProperty));
        Assert.False(editContext.IsValidationFaulted());

        editContext.OnValidationRequestedAsync += (_, _) => Task.FromException(new InvalidOperationException("form failure"));
        Assert.False(await editContext.ValidateAsync());
        Assert.True(editContext.IsValidationFaulted());
        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public async Task FieldValidation_FaultedThenRecovered_ClearsFault()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.ThrowInfraException });

        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => editContext.IsValidationFaulted(field));

        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid });
        validator.GetGate(field);
        editContext.NotifyFieldChanged(field);
        Assert.False(editContext.IsValidationFaulted(field));
        validator.OpenGate(field, ValidationOutcome.Valid);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Empty(editContext.GetValidationMessages(field));
    }

    [Fact]
    public async Task FieldValidation_StaleFaultAfterSupersede_IsIgnored()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var stale = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var current = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.AddValidationTask(field, stale.Task, new CancellationTokenSource());
        editContext.AddValidationTask(field, current.Task, new CancellationTokenSource());

        current.SetResult();
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));
        stale.SetException(new InvalidOperationException("stale failure"));
        await Task.Yield();

        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public async Task OnValidationStateChanged_FieldTask_FiresForStartAndCompletion()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Valid });
        validator.GetGate(field);
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.NotifyFieldChanged(field);
        Assert.Equal(1, notificationCount);

        validator.OpenGate(field, ValidationOutcome.Valid);
        await WaitUntilAsync(() => notificationCount >= 2);

        Assert.False(editContext.IsValidationPending(field));
    }

    [Fact]
    public async Task ValidatorDispose_UnsubscribesHandlers()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.Invalid });
        validator.Dispose();

        editContext.NotifyFieldChanged(field);
        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.Equal(0, validator.FieldValidationStartCount(field));
        Assert.Equal(0, validator.FormValidationStartCount);
    }

    [Fact]
    public void Validate_SyncSubscriber_ReturnsInvalidWhenMessageAdded()
    {
        var editContext = new EditContext(new TestModel());
        var store = new ValidationMessageStore(editContext);
        var field = editContext.Field(nameof(TestModel.StringProperty));
        editContext.OnValidationRequested += (_, _) => store.Add(field, "Required");

        var result = editContext.Validate();

        Assert.False(result);
        Assert.Equal(new[] { "Required" }, editContext.GetValidationMessages(field));
    }

    [Fact]
    public void Validate_AsyncHandlerCompletesSynchronously_IsDrained()
    {
        var editContext = new EditContext(new TestModel());
        var store = new ValidationMessageStore(editContext);
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var handlerRan = false;
        editContext.OnValidationRequestedAsync += (_, _) =>
        {
            handlerRan = true;
            store.Add(field, "Async invalid");
            return Task.CompletedTask;
        };

        var result = editContext.Validate();

        Assert.True(handlerRan);
        Assert.False(result);
        Assert.Equal(new[] { "Async invalid" }, editContext.GetValidationMessages(field));
    }

    [Fact]
    public void Validate_AsyncHandlerIncomplete_ThrowsInvalidOperationException()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => new TaskCompletionSource().Task;

        var exception = Assert.Throws<InvalidOperationException>(() => editContext.Validate());

        Assert.Contains(nameof(EditContext.ValidateAsync), exception.Message);
    }

    [Fact]
    public void Validate_AsyncHandlerThrowsSynchronously_PropagatesOriginalException()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => throw new DivideByZeroException("boom");

        var exception = Assert.Throws<DivideByZeroException>(() => editContext.Validate());

        Assert.Equal("boom", exception.Message);
    }

    [Fact]
    public async Task ValidateAsync_CancelsPendingTasksWithoutAsyncHandlers()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.AddValidationTask(field, tcs.Task, cts);

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.True(cts.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
    }

    [Fact]
    public void AddValidationTask_NullTask_ThrowsArgumentNullException()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));

        Assert.Throws<ArgumentNullException>(() => editContext.AddValidationTask(field, null!, new CancellationTokenSource()));
    }

    [Fact]
    public void AddValidationTask_NullCancellationTokenSource_ThrowsArgumentNullException()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));

        Assert.Throws<ArgumentNullException>(() => editContext.AddValidationTask(field, Task.CompletedTask, null!));
    }

    [Fact]
    public void ValidationState_UnknownField_ReturnsFalse()
    {
        var editContext = new EditContext(new TestModel());
        var field = new FieldIdentifier(new TestModel(), nameof(TestModel.StringProperty));

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public async Task ValidateAsync_AsyncHandlerReturnsFaultedTask_IsContained()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => Task.FromException(new InvalidOperationException("failure"));
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
        Assert.True(notificationCount >= 2);
    }

    [Fact]
    public async Task ValidateAsync_PreAwaitThrow_IsNormalizedToFaultedState()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += ThrowBeforeReturningTask;

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());

        static Task ThrowBeforeReturningTask(object sender, ValidationRequestedEventArgs args)
            => throw new InvalidOperationException("pre-await");
    }

    [Fact]
    public async Task ValidateAsync_HandlerCancellation_IsContainedAndNotFaulted()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => Task.FromCanceled(new CancellationToken(true));

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_SuccessAfterFault_ClearsFormFaultedState()
    {
        var editContext = new EditContext(new TestModel());
        Func<object, ValidationRequestedEventArgs, Task> handler = (_, _) => Task.FromException(new InvalidOperationException("failure"));
        editContext.OnValidationRequestedAsync += handler;
        Assert.False(await editContext.ValidateAsync());
        editContext.OnValidationRequestedAsync -= handler;
        editContext.OnValidationRequestedAsync += (_, _) => Task.CompletedTask;

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_OneOfMultipleHandlersFaults_OtherHandlersStillRun()
    {
        var editContext = new EditContext(new TestModel());
        var otherCompleted = false;
        editContext.OnValidationRequestedAsync += (_, _) => Task.FromException(new InvalidOperationException("failure"));
        editContext.OnValidationRequestedAsync += async (_, _) =>
        {
            await Task.Yield();
            otherCompleted = true;
        };

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
        Assert.True(otherCompleted);
    }

    [Fact]
    public async Task ValidateAsync_OneHandlerCancelsAndAnotherFaults_MarksFormFaulted()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => Task.FromCanceled(new CancellationToken(true));
        editContext.OnValidationRequestedAsync += (_, _) => Task.FromException(new InvalidOperationException("failure"));

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_SyncValidationRequestedThrows_PropagatesException()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequested += (_, _) => throw new DivideByZeroException("sync failure");

        var exception = await Assert.ThrowsAsync<DivideByZeroException>(() => editContext.ValidateAsync());

        Assert.Equal("sync failure", exception.Message);
        Assert.False(editContext.IsValidationPending());
    }

    [Fact]
    public async Task IsValidationFaulted_FieldFaultDoesNotSetParameterlessOverload()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        editContext.AddValidationTask(field, Task.FromException(new InvalidOperationException("failure")), new CancellationTokenSource());

        await WaitUntilAsync(() => editContext.IsValidationFaulted(field));

        Assert.True(editContext.IsValidationFaulted(field));
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_DefaultToken_ExposesNonCancelableToken()
    {
        var editContext = new EditContext(new TestModel());
        CancellationToken captured = default;
        editContext.OnValidationRequestedAsync += (_, args) =>
        {
            captured = args.CancellationToken;
            return Task.CompletedTask;
        };

        Assert.True(await editContext.ValidateAsync());

        Assert.False(captured.CanBeCanceled);
    }

    [Fact]
    public async Task ValidateAsync_CallerToken_IsExposedToAsyncHandlers()
    {
        var editContext = new EditContext(new TestModel());
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;
        editContext.OnValidationRequestedAsync += (_, args) =>
        {
            captured = args.CancellationToken;
            return Task.CompletedTask;
        };

        Assert.True(await editContext.ValidateAsync(cts.Token));

        Assert.True(captured.CanBeCanceled);
        Assert.Equal(cts.Token, captured);
    }

    [Fact]
    public async Task ValidateAsync_CallerCancelsMidFlight_ThrowsOperationCanceledException()
    {
        var editContext = new EditContext(new TestModel());
        using var cts = new CancellationTokenSource();
        editContext.OnValidationRequestedAsync += async (_, args) => await Task.Delay(Timeout.Infinite, args.CancellationToken);

        var task = editContext.ValidateAsync(cts.Token);
        await WaitUntilAsync(() => editContext.IsValidationPending());
        cts.Cancel();

        var exception = await Record.ExceptionAsync(() => task);
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.False(editContext.IsValidationPending());
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_AlreadyCanceledToken_ThrowsBeforeHandlersOrPendingCancellation()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var pendingCts = new CancellationTokenSource();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.AddValidationTask(field, pending.Task, pendingCts);
        var syncCount = 0;
        var asyncCount = 0;
        editContext.OnValidationRequested += (_, _) => syncCount++;
        editContext.OnValidationRequestedAsync += (_, _) =>
        {
            asyncCount++;
            return Task.CompletedTask;
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => editContext.ValidateAsync(cts.Token));

        Assert.Equal(0, syncCount);
        Assert.Equal(0, asyncCount);
        Assert.False(pendingCts.IsCancellationRequested);
        Assert.True(editContext.IsValidationPending(field));
    }

    [Fact]
    public async Task ValidateAsync_HandlerInternalOperationCanceledException_IsContained()
    {
        var editContext = new EditContext(new TestModel());
        using var handlerCts = new CancellationTokenSource();
        handlerCts.Cancel();
        editContext.OnValidationRequestedAsync += async (_, _) =>
        {
            await Task.Yield();
            throw new OperationCanceledException(handlerCts.Token);
        };

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_CancelPendingFieldTask_FiresStateChangedNotification()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.AddValidationTask(field, pending.Task, new CancellationTokenSource());
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        Assert.True(await editContext.ValidateAsync());

        Assert.True(notificationCount >= 3);
        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public async Task ValidateAsync_NoHandlers_FiresStartAndEndNotifications()
    {
        var editContext = new EditContext(new TestModel());
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        Assert.True(await editContext.ValidateAsync());

        Assert.Equal(2, notificationCount);
        Assert.False(editContext.IsValidationPending());
    }

    [Fact]
    public async Task IsValidationPending_FormPass_IsTrueOnlyDuringValidateAsync()
    {
        var editContext = new EditContext(new TestModel());
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.OnValidationRequestedAsync += async (_, _) => await gate.Task;

        var task = editContext.ValidateAsync();
        await WaitUntilAsync(() => editContext.IsValidationPending());

        Assert.True(editContext.IsValidationPending());

        gate.SetResult();
        Assert.True(await task.WaitAsync(DefaultTimeout));
        Assert.False(editContext.IsValidationPending());
    }

    [Fact]
    public void IsValidationPending_FieldTask_DoesNotSetParameterlessOverload()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        editContext.AddValidationTask(field, new TaskCompletionSource().Task, new CancellationTokenSource());

        Assert.True(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationPending());
    }

    [Fact]
    public async Task IsValidationFaulted_CallerCanceledRevalidation_PreservesPreviousFault()
    {
        var editContext = new EditContext(new TestModel());
        Func<object, ValidationRequestedEventArgs, Task> faulting = (_, _) => Task.FromException(new InvalidOperationException("failure"));
        editContext.OnValidationRequestedAsync += faulting;
        Assert.False(await editContext.ValidateAsync());
        editContext.OnValidationRequestedAsync -= faulting;
        using var cts = new CancellationTokenSource();
        editContext.OnValidationRequestedAsync += async (_, args) => await Task.Delay(Timeout.Infinite, args.CancellationToken);

        var task = editContext.ValidateAsync(cts.Token);
        await WaitUntilAsync(() => editContext.IsValidationPending());
        cts.Cancel();

        var exception = await Record.ExceptionAsync(() => task);
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.True(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task IsValidationFaulted_DoesNotClearUntilSuccessfulRevalidationCompletes()
    {
        var editContext = new EditContext(new TestModel());
        Func<object, ValidationRequestedEventArgs, Task> faulting = (_, _) => Task.FromException(new InvalidOperationException("failure"));
        editContext.OnValidationRequestedAsync += faulting;
        Assert.False(await editContext.ValidateAsync());
        editContext.OnValidationRequestedAsync -= faulting;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new List<(bool Pending, bool Faulted)>();
        editContext.OnValidationRequestedAsync += async (_, _) => await gate.Task;
        editContext.OnValidationStateChanged += (_, _) => observed.Add((editContext.IsValidationPending(), editContext.IsValidationFaulted()));

        var task = editContext.ValidateAsync();
        await WaitUntilAsync(() => editContext.IsValidationPending());

        Assert.True(editContext.IsValidationFaulted());
        Assert.DoesNotContain(observed, item => item.Pending && !item.Faulted);

        gate.SetResult();
        Assert.True(await task.WaitAsync(DefaultTimeout));
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_ClearsLingeringFieldFaultsOnEntry()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        editContext.AddValidationTask(field, Task.FromException(new InvalidOperationException("failure")), new CancellationTokenSource());
        await WaitUntilAsync(() => editContext.IsValidationFaulted(field));
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        Assert.True(await editContext.ValidateAsync());

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.True(notificationCount >= 3);
    }

    [Fact]
    public void AddValidationTask_CompletedSuccessfulTask_DoesNotParkSlotOrNotify()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var cts = new CancellationTokenSource();
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.AddValidationTask(field, Task.CompletedTask, cts);

        Assert.Equal(0, notificationCount);
        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Throws<ObjectDisposedException>(() => cts.Cancel());
    }

    [Fact]
    public void AddValidationTask_CompletedFaultedTask_SetsFaultWithoutParkingSlot()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var cts = new CancellationTokenSource();
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.AddValidationTask(field, Task.FromException(new InvalidOperationException("failure")), cts);

        Assert.Equal(1, notificationCount);
        Assert.False(editContext.IsValidationPending(field));
        Assert.True(editContext.IsValidationFaulted(field));
        Assert.Throws<ObjectDisposedException>(() => cts.Cancel());
    }

    [Fact]
    public void AddValidationTask_CompletedCanceledTask_DoesNotFaultOrNotify()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var canceledCts = new CancellationTokenSource();
        canceledCts.Cancel();
        var ownerCts = new CancellationTokenSource();
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.AddValidationTask(field, Task.FromCanceled(canceledCts.Token), ownerCts);

        Assert.Equal(0, notificationCount);
        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Throws<ObjectDisposedException>(() => ownerCts.Cancel());
    }

    [Fact]
    public async Task AddValidationTask_CompletedFaultedTaskSupersedesPendingTask()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var pendingCts = new CancellationTokenSource();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.AddValidationTask(field, pending.Task, pendingCts);
        Assert.True(editContext.IsValidationPending(field));

        editContext.AddValidationTask(field, Task.FromException(new InvalidOperationException("failure")), new CancellationTokenSource());

        // The supersede must clear the slot synchronously so the field is not reported as both
        // pending (stale prior task) and faulted (new completed task) at the same time.
        Assert.False(editContext.IsValidationPending(field));
        Assert.True(pendingCts.IsCancellationRequested);
        Assert.True(editContext.IsValidationFaulted(field));
        pending.SetResult();
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.True(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public void AddValidationTask_CompletedSuccessfulTaskSupersedesPendingTask()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var pendingCts = new CancellationTokenSource();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.AddValidationTask(field, pending.Task, pendingCts);
        Assert.True(editContext.IsValidationPending(field));

        editContext.AddValidationTask(field, Task.CompletedTask, new CancellationTokenSource());

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.True(pendingCts.IsCancellationRequested);
        pending.SetResult();
    }

    [Fact]
    public void AddValidationTask_CompletedSuccessfulTaskSupersedesPending_ClearsPriorFault()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        // Seed a faulted state, then park a new pending task on top so we can verify that a
        // completed-success supersede resets state cleanly.
        editContext.AddValidationTask(field, Task.FromException(new InvalidOperationException("seed")), new CancellationTokenSource());
        Assert.True(editContext.IsValidationFaulted(field));

        using var pendingCts = new CancellationTokenSource();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.AddValidationTask(field, pending.Task, pendingCts);
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.True(editContext.IsValidationPending(field));

        editContext.AddValidationTask(field, Task.CompletedTask, new CancellationTokenSource());

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        pending.SetResult();
    }

    [Fact]
    public void AddValidationTask_CompletedSuccessfulTask_ClearsPriorFaultFromCompletedFaultedTask()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;

        // Fast-path settle a faulted task to mark the field as faulted.
        editContext.AddValidationTask(field, Task.FromException(new InvalidOperationException("seed")), new CancellationTokenSource());
        Assert.True(editContext.IsValidationFaulted(field));
        var notificationsAfterFault = notifications;

        // Fast-path settle a successful task and verify the fault flag is cleared and a
        // notification was emitted because the flag changed.
        editContext.AddValidationTask(field, Task.CompletedTask, new CancellationTokenSource());

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Equal(notificationsAfterFault + 1, notifications);
    }

    [Fact]
    public void AddValidationTask_CompletedCancelledTask_ClearsPriorFaultFromCompletedFaultedTask()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));

        editContext.AddValidationTask(field, Task.FromException(new InvalidOperationException("seed")), new CancellationTokenSource());
        Assert.True(editContext.IsValidationFaulted(field));

        editContext.AddValidationTask(field, Task.FromCanceled(new CancellationToken(canceled: true)), new CancellationTokenSource());

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public void AddValidationTask_CompletedSuccessfulTask_DoesNotNotifyWhenStateUnchanged()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;

        // Field has neither pending nor faulted state; a completed-success task should be a no-op.
        editContext.AddValidationTask(field, Task.CompletedTask, new CancellationTokenSource());

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Equal(0, notifications);
    }

    [Fact]
    public void AddValidationTask_CompletedFaultedTask_ObservesException()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var fired = false;
        EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, args) =>
        {
            fired = true;
            args.SetObserved();
        };
        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            var task = Task.FromException(new InvalidOperationException("failure"));
            editContext.AddValidationTask(field, task, new CancellationTokenSource());
            task = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(fired);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= handler;
        }
    }

    [Fact]
    public async Task ValidateAsync_CallerCancelsButHandlersCompleted_ThrowsOperationCanceledException()
    {
        // Verifies that caller-token cancellation propagates even when no handler observed
        // the token (Task.WhenAll completed successfully). The post-await
        // ThrowIfCancellationRequested() guarantees the caller's intent is honored.
        var editContext = new EditContext(new TestModel());
        using var cts = new CancellationTokenSource();
        var handlerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerExit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.OnValidationRequestedAsync += async (_, _) =>
        {
            handlerEntered.SetResult();
            await handlerExit.Task;
        };

        var task = editContext.ValidateAsync(cts.Token);
        await handlerEntered.Task;
        cts.Cancel();
        handlerExit.SetResult();

        var exception = await Record.ExceptionAsync(() => task);
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.False(editContext.IsValidationPending());
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_AsyncHandlerReturnsNull_TreatedAsCompletedTask()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => null;

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_AsyncHandlerThrowsOperationCanceledExceptionSync_DoesNotMarkFormAsFaulted()
    {
        // Sync throw of OCE before the handler's first await must be classified as cancellation,
        // not as a fault, matching the post-await OCE semantics.
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, args) =>
        {
            throw new OperationCanceledException();
        };

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.False(editContext.IsValidationFaulted());
    }

    [Fact]
    public async Task ValidateAsync_OneAsyncHandlerThrowsOceSync_OtherHandlerFaults_IsClassifiedAsFault()
    {
        // Cross-check: a sync OCE from one handler must not mask a real fault from another.
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => throw new OperationCanceledException();
        editContext.OnValidationRequestedAsync += (_, _) => Task.FromException(new InvalidOperationException("boom"));

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
    }

    [Fact]
    public void Validate_AsyncHandlerReturnsNull_TreatedAsCompletedTask()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequestedAsync += (_, _) => null;

        var result = editContext.Validate();

        Assert.True(result);
        Assert.False(editContext.IsValidationFaulted());
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

        public string OtherProperty { get; set; }
    }
}
