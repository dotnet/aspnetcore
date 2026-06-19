// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class EditContextAsyncTest
{
    [Fact]
    public Task FieldValidation_Valid_CompletesWithoutMessages() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task FieldValidation_Invalid_AddsMessage() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task FieldValidation_InfrastructureException_MarksFieldFaulted() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task FieldValidation_OperationCanceledFromUnrelatedSource_MarksFieldFaulted() => RunOnDispatcher(async () =>
    {
        // OperationCanceledException thrown from inside a validator for a reason unrelated to our
        // cancellation (e.g. an HttpClient timeout or a user CancellationTokenSource on a DB query)
        // should be treated as an infrastructure fault, not silently swallowed. Only OCEs that
        // result from our own CTS cancellation are silent.
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig
        {
            ObserveCancellation = false,
            Custom = async (_, _) =>
            {
                // Yield so we go through the ObserveFieldValidationTask path (rather than
                // settling synchronously via TrackFieldValidation's fast path).
                await Task.Yield();
                using var unrelated = new CancellationTokenSource();
                unrelated.Cancel();
                throw new OperationCanceledException(unrelated.Token);
            },
        });

        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.True(editContext.IsValidationFaulted(field));
        Assert.Empty(editContext.GetValidationMessages(field));
    });

    [Fact]
    public Task FieldValidation_Reedit_CancelsPreviousTask() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task ValidateAsync_CancelsPendingFieldTask() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task FieldValidation_MultipleFields_CompleteIndependently() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task ValidateAsync_AllFieldsValid_ReturnsTrue() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(editContext.Field(nameof(TestModel.StringProperty)), new ValidationConfig { Outcome = ValidationOutcome.Valid });
        validator.Configure(editContext.Field(nameof(TestModel.OtherProperty)), new ValidationConfig { Outcome = ValidationOutcome.Valid });

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.Empty(editContext.GetValidationMessages());
        Assert.False(editContext.IsValidationFaulted());
    });

    [Fact]
    public Task ValidateAsync_FieldInvalid_ReturnsFalseAndAddsMessage() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task ValidateAsync_FieldValidatorThrows_ReturnsFalseAndMarksFormFaulted() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var validator = new TestAsyncValidator(editContext);
        validator.Configure(field, new ValidationConfig { Outcome = ValidationOutcome.ThrowInfraException });

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
        Assert.False(editContext.IsValidationFaulted(field));
    });

    [Fact]
    public Task ValidateAsync_RegisteredAsyncTaskThrowsBeforeAwait_IsContainedAsFault() => RunOnDispatcher(async () =>
    {
        // A validator starts its work with an async method, so an exception thrown before the
        // first await is captured into the returned task and observed as a fault rather than
        // propagating synchronously out of the handler.
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(ThrowBeforeAwaitAsync(shouldThrow: true));

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());

        static async Task ThrowBeforeAwaitAsync(bool shouldThrow)
        {
            if (shouldThrow)
            {
                throw new InvalidOperationException("failure");
            }

            await Task.CompletedTask;
        }
    });

    [Fact]
    public Task ValidateAsync_MultipleValidators_AwaitsBothAndCombinesMessages() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task IsValidationPending_Overloads_SeparateFieldAndFormState() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task IsValidationFaulted_Overloads_SeparateFieldAndFormState() => RunOnDispatcher(async () =>
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.TrackFieldValidation(field, _ => tcs.Task);

        tcs.SetException(new InvalidOperationException("field failure"));
        await WaitUntilAsync(() => editContext.IsValidationFaulted(field));

        Assert.True(editContext.IsValidationFaulted(() => model.StringProperty));
        Assert.False(editContext.IsValidationFaulted());

        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("form failure")));
        Assert.False(await editContext.ValidateAsync());
        Assert.True(editContext.IsValidationFaulted());
        Assert.False(editContext.IsValidationFaulted(field));
    });

    [Fact]
    public Task FieldValidation_FaultedThenRecovered_ClearsFault() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task FieldValidation_StaleFaultAfterSupersede_IsIgnored() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var stale = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var current = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.TrackFieldValidation(field, _ => stale.Task);
        editContext.TrackFieldValidation(field, _ => current.Task);

        current.SetResult();
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));
        stale.SetException(new InvalidOperationException("stale failure"));
        await Task.Yield();

        Assert.False(editContext.IsValidationFaulted(field));
    });

    [Fact]
    public Task OnValidationStateChanged_FieldTask_FiresForStartAndCompletion() => RunOnDispatcher(async () =>
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
    });

    [Fact]
    public Task ValidatorDispose_UnsubscribesHandlers() => RunOnDispatcher(async () =>
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
    });

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
        editContext.OnValidationRequested += (_, args) =>
        {
            handlerRan = true;
            store.Add(field, "Async invalid");
            args.AddValidationTask(Task.CompletedTask);
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
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(new TaskCompletionSource().Task);

        var exception = Assert.Throws<InvalidOperationException>(() => editContext.Validate());

        Assert.Contains(nameof(EditContext.ValidateAsync), exception.Message);
    }

    [Fact]
    public void Validate_AsyncHandlerThrowsSynchronously_PropagatesOriginalException()
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequested += (_, _) => throw new DivideByZeroException("boom");

        var exception = Assert.Throws<DivideByZeroException>(() => editContext.Validate());

        Assert.Equal("boom", exception.Message);
    }

    [Fact]
    public void Validate_RegisteredFaultedTask_ContainsFaultAndReturnsFalse()
    {
        // A completed-faulted registered task is contained the same way ValidateAsync contains it:
        // the form is marked faulted and Validate returns false rather than rethrowing.
        var editContext = new EditContext(new TestModel());
        var thrown = new InvalidOperationException("infra failure");
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromException(thrown));

        var result = editContext.Validate();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
        Assert.Same(thrown, editContext.GetValidationException());
    }

    [Fact]
    public void Validate_SuccessfulPass_ClearsPriorFormFault()
    {
        var editContext = new EditContext(new TestModel());
        EventHandler<ValidationRequestedEventArgs> faulting = (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("failure")));
        editContext.OnValidationRequested += faulting;
        Assert.False(editContext.Validate());
        Assert.NotNull(editContext.GetValidationException());

        editContext.OnValidationRequested -= faulting;

        Assert.True(editContext.Validate());
        Assert.False(editContext.IsValidationFaulted());
        Assert.Null(editContext.GetValidationException());
    }

    [Fact]
    public Task ValidateAsync_CancelsPendingTasksWithoutAsyncHandlers() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken capturedToken = default;
        editContext.TrackFieldValidation(field, token =>
        {
            capturedToken = token;
            return tcs.Task;
        });

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.True(capturedToken.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
    });

    [Fact]
    public void TrackFieldValidation_NullValidate_ThrowsArgumentNullException()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));

        Assert.Throws<ArgumentNullException>(() => editContext.TrackFieldValidation(field, null!));
    }

    [Fact]
    public Task TrackFieldValidation_FactoryThrows_SupersedesPriorPendingValidation() => RunOnDispatcher(async () =>
    {
        // If the validate factory throws, the prior pending validation must still be superseded
        // (cancelled and cleared) so the field does not stay stuck in the pending state.
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        CancellationToken priorToken = default;
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.TrackFieldValidation(field, token =>
        {
            priorToken = token;
            token.Register(() => pending.TrySetCanceled(token));
            return pending.Task;
        });
        Assert.True(editContext.IsValidationPending(field));

        Assert.Throws<InvalidOperationException>(
            () => editContext.TrackFieldValidation(field, _ => throw new InvalidOperationException("factory failure")));

        Assert.True(priorToken.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));

        await WaitUntilAsync(() => pending.Task.IsCompleted);
    });

    [Fact]
    public Task TrackFieldValidation_FactoryReturnsNull_SupersedesPriorPendingValidation() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        CancellationToken priorToken = default;
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.TrackFieldValidation(field, token =>
        {
            priorToken = token;
            token.Register(() => pending.TrySetCanceled(token));
            return pending.Task;
        });
        Assert.True(editContext.IsValidationPending(field));

        Assert.Throws<ArgumentNullException>(() => editContext.TrackFieldValidation(field, _ => null!));

        Assert.True(priorToken.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));

        await WaitUntilAsync(() => pending.Task.IsCompleted);
    });

    [Fact]
    public void ValidationRequestedEventArgs_AddValidationTask_NullTask_ThrowsArgumentNullException()
    {
        var args = new ValidationRequestedEventArgs();

        Assert.Throws<ArgumentNullException>(() => args.AddValidationTask(null!));
    }

    [Fact]
    public void ValidationRequestedEventArgs_Empty_AddValidationTask_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => ValidationRequestedEventArgs.Empty.AddValidationTask(Task.CompletedTask));
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
    public Task ValidateAsync_AsyncHandlerReturnsFaultedTask_IsContained() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("failure")));
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
        Assert.True(notificationCount >= 2);
    });

    [Fact]
    public Task ValidateAsync_HandlerCancellationFromUnrelatedSource_MarksFormFaulted() => RunOnDispatcher(async () =>
    {
        // A registered task that completed Canceled by a source other than the caller's token
        // (here an arbitrary already-cancelled token) is an infrastructure fault, not a benign
        // outcome, mirroring the per-field treatment of unrelated cancellation.
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromCanceled(new CancellationToken(true)));

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
        Assert.IsType<TaskCanceledException>(editContext.GetValidationException());
    });

    [Fact]
    public Task ValidateAsync_SuccessAfterFault_ClearsFormFaultedState() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        EventHandler<ValidationRequestedEventArgs> handler = (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("failure")));
        editContext.OnValidationRequested += handler;
        Assert.False(await editContext.ValidateAsync());
        editContext.OnValidationRequested -= handler;
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.CompletedTask);

        var result = await editContext.ValidateAsync();

        Assert.True(result);
        Assert.False(editContext.IsValidationFaulted());
    });

    [Fact]
    public Task ValidateAsync_OneOfMultipleHandlersFaults_OtherHandlersStillRun() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var otherCompleted = false;
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("failure")));
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(MarkCompletedAsync());

        async Task MarkCompletedAsync()
        {
            await Task.Yield();
            otherCompleted = true;
        }

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
        Assert.True(otherCompleted);
    });

    [Fact]
    public Task ValidateAsync_OneHandlerCancelsAndAnotherFaults_MarksFormFaulted() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromCanceled(new CancellationToken(true)));
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("failure")));

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
    });

    [Fact]
    public Task ValidateAsync_SyncValidationRequestedThrows_PropagatesException() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        editContext.OnValidationRequested += (_, _) => throw new DivideByZeroException("sync failure");

        var exception = await Assert.ThrowsAsync<DivideByZeroException>(() => editContext.ValidateAsync());

        Assert.Equal("sync failure", exception.Message);
        Assert.False(editContext.IsValidationPending());
    });

    [Fact]
    public Task ValidateAsync_SyncHandlerThrowsAfterTaskRegistered_PropagatesAndClearsPending() => RunOnDispatcher(async () =>
    {
        // When a later sync handler throws, the multicast chain aborts. The framework observes the
        // task an earlier handler already registered (so a later fault does not surface as an
        // UnobservedTaskException) and rethrows so the exception reaches the caller.
        var editContext = new EditContext(new TestModel());
        var registered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(registered.Task);
        editContext.OnValidationRequested += (_, _) => throw new InvalidOperationException("sync failure");

        var exception = await Record.ExceptionAsync(() => editContext.ValidateAsync());

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("sync failure", exception.Message);
        Assert.False(editContext.IsValidationPending());

        registered.SetException(new InvalidOperationException("orphan fault"));
        await Task.Yield();
    });

    [Fact]
    public Task IsValidationFaulted_FieldFaultDoesNotSetParameterlessOverload() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        editContext.TrackFieldValidation(field, _ => Task.FromException(new InvalidOperationException("failure")));

        await WaitUntilAsync(() => editContext.IsValidationFaulted(field));

        Assert.True(editContext.IsValidationFaulted(field));
        Assert.False(editContext.IsValidationFaulted());
    });

    [Fact]
    public Task ValidateAsync_DefaultToken_ExposesNonCancelableToken() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        CancellationToken captured = default;
        editContext.OnValidationRequested += (_, args) => captured = args.CancellationToken;

        Assert.True(await editContext.ValidateAsync());

        Assert.False(captured.CanBeCanceled);
    });

    [Fact]
    public Task ValidateAsync_CallerToken_IsExposedToHandlers() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;
        editContext.OnValidationRequested += (_, args) => captured = args.CancellationToken;

        Assert.True(await editContext.ValidateAsync(cts.Token));

        Assert.True(captured.CanBeCanceled);
        Assert.Equal(cts.Token, captured);
    });

    [Fact]
    public Task ValidateAsync_CallerCancelsMidFlight_ThrowsOperationCanceledException() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        using var cts = new CancellationTokenSource();
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.Delay(Timeout.Infinite, args.CancellationToken));

        var task = editContext.ValidateAsync(cts.Token);
        await WaitUntilAsync(() => editContext.IsValidationPending());
        cts.Cancel();

        var exception = await Record.ExceptionAsync(() => task);
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.False(editContext.IsValidationPending());
        Assert.False(editContext.IsValidationFaulted());
    });

    [Fact]
    public Task ValidateAsync_AlreadyCanceledToken_RunsHandlersThenThrows() => RunOnDispatcher(async () =>
    {
        // Caller-provided already-cancelled token: ValidateAsync still cancels superseded
        // per-field tasks and invokes the registered handlers before honoring cancellation
        // at the post-await ThrowIfCancellationRequested. This matches the typical pattern
        // where "the cancellation is in progress, not 'we never started'".
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken capturedToken = default;
        editContext.TrackFieldValidation(field, token =>
        {
            capturedToken = token;
            return pending.Task;
        });
        var syncCount = 0;
        var asyncCount = 0;
        editContext.OnValidationRequested += (_, _) => syncCount++;
        editContext.OnValidationRequested += (_, args) =>
        {
            asyncCount++;
            args.AddValidationTask(Task.CompletedTask);
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => editContext.ValidateAsync(cts.Token));

        Assert.Equal(1, syncCount);
        Assert.Equal(1, asyncCount);
        Assert.True(capturedToken.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
    });

    [Fact]
    public Task ValidateAsync_HandlerInternalOperationCanceledException_MarksFormFaulted() => RunOnDispatcher(async () =>
    {
        // An OperationCanceledException thrown by a validator from its own token (unrelated to the
        // caller's token) is an infrastructure fault, not a benign cancellation.
        var editContext = new EditContext(new TestModel());
        using var handlerCts = new CancellationTokenSource();
        handlerCts.Cancel();
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(ThrowOceAsync());

        async Task ThrowOceAsync()
        {
            await Task.Yield();
            throw new OperationCanceledException(handlerCts.Token);
        }

        var result = await editContext.ValidateAsync();

        Assert.False(result);
        Assert.True(editContext.IsValidationFaulted());
    });

    [Fact]
    public Task ValidateAsync_CancelPendingFieldTask_FiresStateChangedNotification() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.TrackFieldValidation(field, _ => pending.Task);
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        Assert.True(await editContext.ValidateAsync());

        Assert.True(notificationCount >= 3);
        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
    });

    [Fact]
    public Task ValidateAsync_NoHandlers_FiresStartAndEndNotifications() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        Assert.True(await editContext.ValidateAsync());

        Assert.Equal(2, notificationCount);
        Assert.False(editContext.IsValidationPending());
    });

    [Fact]
    public Task IsValidationPending_FormPass_IsTrueOnlyDuringValidateAsync() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(gate.Task);

        var task = editContext.ValidateAsync();
        await WaitUntilAsync(() => editContext.IsValidationPending());

        Assert.True(editContext.IsValidationPending());

        gate.SetResult();
        Assert.True(await task.WaitAsync(DefaultTimeout));
        Assert.False(editContext.IsValidationPending());
    });

    [Fact]
    public void IsValidationPending_FieldTask_DoesNotSetParameterlessOverload()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var pendingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration pendingRegistration = default;
        editContext.TrackFieldValidation(field, token =>
        {
            pendingRegistration = token.Register(() => pendingTcs.TrySetCanceled(token));
            return pendingTcs.Task;
        });

        Assert.True(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationPending());

        editContext.TrackFieldValidation(field, _ => Task.CompletedTask);
        pendingRegistration.Dispose();
    }

    [Fact]
    public Task IsValidationFaulted_CallerCanceledRevalidation_PreservesPreviousFault() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        EventHandler<ValidationRequestedEventArgs> faulting = (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("failure")));
        editContext.OnValidationRequested += faulting;
        Assert.False(await editContext.ValidateAsync());
        editContext.OnValidationRequested -= faulting;
        using var cts = new CancellationTokenSource();
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.Delay(Timeout.Infinite, args.CancellationToken));

        var task = editContext.ValidateAsync(cts.Token);
        await WaitUntilAsync(() => editContext.IsValidationPending());
        cts.Cancel();

        var exception = await Record.ExceptionAsync(() => task);
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.True(editContext.IsValidationFaulted());
    });

    [Fact]
    public Task IsValidationFaulted_DoesNotClearUntilSuccessfulRevalidationCompletes() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        EventHandler<ValidationRequestedEventArgs> faulting = (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("failure")));
        editContext.OnValidationRequested += faulting;
        Assert.False(await editContext.ValidateAsync());
        editContext.OnValidationRequested -= faulting;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new List<(bool Pending, bool Faulted)>();
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(gate.Task);
        editContext.OnValidationStateChanged += (_, _) => observed.Add((editContext.IsValidationPending(), editContext.IsValidationFaulted()));

        var task = editContext.ValidateAsync();
        await WaitUntilAsync(() => editContext.IsValidationPending());

        Assert.True(editContext.IsValidationFaulted());
        Assert.DoesNotContain(observed, item => item.Pending && !item.Faulted);

        gate.SetResult();
        Assert.True(await task.WaitAsync(DefaultTimeout));
        Assert.False(editContext.IsValidationFaulted());
    });

    [Fact]
    public Task ValidateAsync_ClearsLingeringFieldFaultsOnEntry() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        editContext.TrackFieldValidation(field, _ => Task.FromException(new InvalidOperationException("failure")));
        await WaitUntilAsync(() => editContext.IsValidationFaulted(field));
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        Assert.True(await editContext.ValidateAsync());

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.True(notificationCount >= 3);
    });

    [Fact]
    public void TrackFieldValidation_CompletedSuccessfulTask_DoesNotParkSlotOrNotify()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.TrackFieldValidation(field, _ => Task.CompletedTask);

        Assert.Equal(0, notificationCount);
        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public void TrackFieldValidation_CompletedFaultedTask_SetsFaultWithoutParkingSlot()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.TrackFieldValidation(field, _ => Task.FromException(new InvalidOperationException("failure")));

        Assert.Equal(1, notificationCount);
        Assert.False(editContext.IsValidationPending(field));
        Assert.True(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public void TrackFieldValidation_CompletedCanceledByUnrelatedToken_MarksFieldFaulted()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var unrelatedCts = new CancellationTokenSource();
        unrelatedCts.Cancel();
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.TrackFieldValidation(field, _ => Task.FromCanceled(unrelatedCts.Token));

        Assert.Equal(1, notificationCount);
        Assert.False(editContext.IsValidationPending(field));
        Assert.True(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public void GetValidationException_CompletedFaultedTask_ReturnsUnwrappedException()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var thrown = new InvalidOperationException("failure");

        editContext.TrackFieldValidation(field, _ => Task.FromException(thrown));

        Assert.True(editContext.IsValidationFaulted(field));
        Assert.Same(thrown, editContext.GetValidationException(field));
    }

    [Fact]
    public void GetValidationException_CompletedCanceledByUnrelatedToken_ReturnsTaskCanceledException()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        using var unrelatedCts = new CancellationTokenSource();
        unrelatedCts.Cancel();

        editContext.TrackFieldValidation(field, _ => Task.FromCanceled(unrelatedCts.Token));

        Assert.True(editContext.IsValidationFaulted(field));
        Assert.IsType<TaskCanceledException>(editContext.GetValidationException(field));
    }

    [Fact]
    public void GetValidationException_NotFaultedField_ReturnsNull()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));

        editContext.TrackFieldValidation(field, _ => Task.CompletedTask);

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Null(editContext.GetValidationException(field));
    }

    [Fact]
    public Task GetValidationException_FieldTaskFaultsAfterAwait_ReturnsThrownException() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var thrown = new InvalidOperationException("infra failure");
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.TrackFieldValidation(field, _ => tcs.Task);

        tcs.SetException(thrown);
        await WaitUntilAsync(() => editContext.IsValidationFaulted(field));

        Assert.Same(thrown, editContext.GetValidationException(field));
    });

    [Fact]
    public Task GetValidationException_FieldRecoversAfterFault_ReturnsNull() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        editContext.TrackFieldValidation(field, _ => Task.FromException(new InvalidOperationException("seed")));
        Assert.NotNull(editContext.GetValidationException(field));

        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.TrackFieldValidation(field, _ => recovered.Task);
        recovered.SetResult();
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Null(editContext.GetValidationException(field));
    });

    [Fact]
    public Task GetValidationException_FormSingleHandlerFaults_ReturnsThrownException() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var thrown = new InvalidOperationException("form failure");
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromException(thrown));

        Assert.False(await editContext.ValidateAsync());

        Assert.True(editContext.IsValidationFaulted());
        Assert.Same(thrown, editContext.GetValidationException());
    });

    [Fact]
    public Task GetValidationException_FormMultipleHandlersFault_ReturnsAggregateException() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var first = new InvalidOperationException("first");
        var second = new InvalidOperationException("second");
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromException(first));
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.FromException(second));

        Assert.False(await editContext.ValidateAsync());

        var fault = Assert.IsType<AggregateException>(editContext.GetValidationException());
        Assert.Contains(first, fault.InnerExceptions);
        Assert.Contains(second, fault.InnerExceptions);
    });

    [Fact]
    public Task GetValidationException_FormSuccessfulPass_ReturnsNull() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        EventHandler<ValidationRequestedEventArgs> faulting = (_, args) => args.AddValidationTask(Task.FromException(new InvalidOperationException("failure")));
        editContext.OnValidationRequested += faulting;
        Assert.False(await editContext.ValidateAsync());
        Assert.NotNull(editContext.GetValidationException());
        editContext.OnValidationRequested -= faulting;
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.CompletedTask);

        Assert.True(await editContext.ValidateAsync());

        Assert.False(editContext.IsValidationFaulted());
        Assert.Null(editContext.GetValidationException());
    });

    [Fact]
    public Task GetValidationException_FormCallerCancelled_PreservesPreviousFault() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var thrown = new InvalidOperationException("failure");
        EventHandler<ValidationRequestedEventArgs> faulting = (_, args) => args.AddValidationTask(Task.FromException(thrown));
        editContext.OnValidationRequested += faulting;
        Assert.False(await editContext.ValidateAsync());
        editContext.OnValidationRequested -= faulting;
        using var cts = new CancellationTokenSource();
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(Task.Delay(Timeout.Infinite, args.CancellationToken));

        var task = editContext.ValidateAsync(cts.Token);
        await WaitUntilAsync(() => editContext.IsValidationPending());
        cts.Cancel();
        await Record.ExceptionAsync(() => task);

        Assert.Same(thrown, editContext.GetValidationException());
    });

    [Fact]
    public Task TrackFieldValidation_CompletedFaultedTaskSupersedesPendingTask() => RunOnDispatcher(async () =>
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken capturedToken = default;
        editContext.TrackFieldValidation(field, token =>
        {
            capturedToken = token;
            return pending.Task;
        });
        Assert.True(editContext.IsValidationPending(field));

        editContext.TrackFieldValidation(field, _ => Task.FromException(new InvalidOperationException("failure")));

        Assert.False(editContext.IsValidationPending(field));
        Assert.True(capturedToken.IsCancellationRequested);
        Assert.True(editContext.IsValidationFaulted(field));
        pending.SetResult();
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.True(editContext.IsValidationFaulted(field));
    });

    [Fact]
    public void TrackFieldValidation_CompletedSuccessfulTaskSupersedesPendingTask()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken capturedToken = default;
        editContext.TrackFieldValidation(field, token =>
        {
            capturedToken = token;
            return pending.Task;
        });
        Assert.True(editContext.IsValidationPending(field));

        editContext.TrackFieldValidation(field, _ => Task.CompletedTask);

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.True(capturedToken.IsCancellationRequested);
        pending.SetResult();
    }

    [Fact]
    public void TrackFieldValidation_CompletedSuccessfulTaskSupersedesPending_ClearsPriorFault()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        editContext.TrackFieldValidation(field, _ => Task.FromException(new InvalidOperationException("seed")));
        Assert.True(editContext.IsValidationFaulted(field));

        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.TrackFieldValidation(field, _ => pending.Task);
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.True(editContext.IsValidationPending(field));

        editContext.TrackFieldValidation(field, _ => Task.CompletedTask);

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        pending.SetResult();
    }

    [Fact]
    public void TrackFieldValidation_CompletedSuccessfulTask_ClearsPriorFaultFromCompletedFaultedTask()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;

        editContext.TrackFieldValidation(field, _ => Task.FromException(new InvalidOperationException("seed")));
        Assert.True(editContext.IsValidationFaulted(field));
        var notificationsAfterFault = notifications;

        editContext.TrackFieldValidation(field, _ => Task.CompletedTask);

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Equal(notificationsAfterFault + 1, notifications);
    }

    [Fact]
    public void TrackFieldValidation_CompletedSuccessfulTask_DoesNotNotifyWhenStateUnchanged()
    {
        var editContext = new EditContext(new TestModel());
        var field = editContext.Field(nameof(TestModel.StringProperty));
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;

        editContext.TrackFieldValidation(field, _ => Task.CompletedTask);

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Equal(0, notifications);
    }

    [Fact]
    public void TrackFieldValidation_CompletedFaultedTask_ObservesException()
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
            editContext.TrackFieldValidation(field, _ => task);
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
    public Task ValidateAsync_CallerCancelsButHandlersCompleted_ThrowsOperationCanceledException() => RunOnDispatcher(async () =>
    {
        // Verifies that caller-token cancellation propagates even when no handler observed
        // the token (Task.WhenAll completed successfully). The post-await
        // ThrowIfCancellationRequested() guarantees the caller's intent is honored.
        var editContext = new EditContext(new TestModel());
        using var cts = new CancellationTokenSource();
        var handlerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerExit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.OnValidationRequested += (_, args) => args.AddValidationTask(HandlerAsync());

        async Task HandlerAsync()
        {
            handlerEntered.SetResult();
            await handlerExit.Task;
        }

        var task = editContext.ValidateAsync(cts.Token);
        await handlerEntered.Task;
        cts.Cancel();
        handlerExit.SetResult();

        var exception = await Record.ExceptionAsync(() => task);
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.False(editContext.IsValidationPending());
        Assert.False(editContext.IsValidationFaulted());
    });

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

    // Runs a test body under Blazor's default dispatcher to simulate the renderer threading model:
    // validator continuations, the framework's ObserveFieldValidationTask continuation, and the test
    // body's polling/assertions all serialize through a single logical thread. This matches
    // EditContext's documented threading model (see EditContext class XML remarks) and removes
    // incidental cross-thread races that would otherwise occur when raw EditContext is exercised on
    // the bare thread pool. Uses the public Dispatcher.CreateDefault() — the same factory the
    // renderer uses in production — consistent with precedent across the Blazor test suite.
    private static Task RunOnDispatcher(Func<Task> body)
        => Dispatcher.CreateDefault().InvokeAsync(body);

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private sealed class TestModel
    {
        public string StringProperty { get; set; }

        public string OtherProperty { get; set; }
    }
}
