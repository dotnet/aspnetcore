// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0618 // Validate() is obsolete — existing tests exercise backward compat

namespace Microsoft.AspNetCore.Components.Forms;

public class EditContextTest
{
    [Fact]
    public void CannotUseNullModel()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new EditContext(null));
        Assert.Equal("model", ex.ParamName);
    }

    [Fact]
    public void CanGetModel()
    {
        var model = new object();
        var editContext = new EditContext(model);
        Assert.Same(model, editContext.Model);
    }

    [Fact]
    public void CanConstructFieldIdentifiersForRootModel()
    {
        // Arrange/Act
        var model = new object();
        var editContext = new EditContext(model);
        var fieldIdentifier = editContext.Field("testFieldName");

        // Assert
        Assert.Same(model, fieldIdentifier.Model);
        Assert.Equal("testFieldName", fieldIdentifier.FieldName);
    }

    [Fact]
    public void IsInitiallyUnmodified()
    {
        var editContext = new EditContext(new object());
        Assert.False(editContext.IsModified());
    }

    [Fact]
    public void TracksFieldsAsModifiedWhenValueChanged()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var fieldOnThisModel1 = editContext.Field("field1");
        var fieldOnThisModel2 = editContext.Field("field2");
        var fieldOnOtherModel = new FieldIdentifier(new object(), "field on other model");

        // Act
        editContext.NotifyFieldChanged(fieldOnThisModel1);
        editContext.NotifyFieldChanged(fieldOnOtherModel);

        // Assert
        Assert.True(editContext.IsModified());
        Assert.True(editContext.IsModified(fieldOnThisModel1));
        Assert.False(editContext.IsModified(fieldOnThisModel2));
        Assert.True(editContext.IsModified(fieldOnOtherModel));
    }

    [Fact]
    public void CanClearIndividualModifications()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var fieldThatWasModified = editContext.Field("field1");
        var fieldThatRemainsModified = editContext.Field("field2");
        var fieldThatWasNeverModified = editContext.Field("field that was never modified");
        editContext.NotifyFieldChanged(fieldThatWasModified);
        editContext.NotifyFieldChanged(fieldThatRemainsModified);

        // Act
        editContext.MarkAsUnmodified(fieldThatWasModified);
        editContext.MarkAsUnmodified(fieldThatWasNeverModified);

        // Assert
        Assert.True(editContext.IsModified());
        Assert.False(editContext.IsModified(fieldThatWasModified));
        Assert.True(editContext.IsModified(fieldThatRemainsModified));
        Assert.False(editContext.IsModified(fieldThatWasNeverModified));
    }

    [Fact]
    public void CanClearAllModifications()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var field1 = editContext.Field("field1");
        var field2 = editContext.Field("field2");
        editContext.NotifyFieldChanged(field1);
        editContext.NotifyFieldChanged(field2);

        // Act
        editContext.MarkAsUnmodified();

        // Assert
        Assert.False(editContext.IsModified());
        Assert.False(editContext.IsModified(field1));
        Assert.False(editContext.IsModified(field2));
    }

    [Fact]
    public void RaisesEventWhenFieldIsChanged()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var field1 = new FieldIdentifier(new object(), "fieldname"); // Shows it can be on a different model
        var didReceiveNotification = false;
        editContext.OnFieldChanged += (sender, eventArgs) =>
        {
            Assert.Same(editContext, sender);
            Assert.Equal(field1, eventArgs.FieldIdentifier);
            didReceiveNotification = true;
        };

        // Act
        editContext.NotifyFieldChanged(field1);

        // Assert
        Assert.True(didReceiveNotification);
    }

    [Fact]
    public void CanEnumerateValidationMessagesAcrossAllStoresForSingleField()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var store1 = new ValidationMessageStore(editContext);
        var store2 = new ValidationMessageStore(editContext);
        var field = new FieldIdentifier(new object(), "field");
        var fieldWithNoState = new FieldIdentifier(new object(), "field with no state");
        store1.Add(field, "Store 1 message 1");
        store1.Add(field, "Store 1 message 2");
        store1.Add(new FieldIdentifier(new object(), "otherfield"), "Message for other field that should not appear in results");
        store2.Add(field, "Store 2 message 1");

        // Act/Assert: Can pick out the messages for a field
        Assert.Equal(new[]
        {
                "Store 1 message 1",
                "Store 1 message 2",
                "Store 2 message 1",
            }, editContext.GetValidationMessages(field).OrderBy(x => x)); // Sort because the order isn't defined

        // Act/Assert: It's fine to ask for messages for a field with no associated state
        Assert.Empty(editContext.GetValidationMessages(fieldWithNoState));

        // Act/Assert: After clearing a single store, we only see the results from other stores
        store1.Clear(field);
        Assert.Equal(new[] { "Store 2 message 1", }, editContext.GetValidationMessages(field));
    }

    [Fact]
    public void CanEnumerateValidationMessagesAcrossAllStoresForAllFields()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var store1 = new ValidationMessageStore(editContext);
        var store2 = new ValidationMessageStore(editContext);
        var field1 = new FieldIdentifier(new object(), "field1");
        var field2 = new FieldIdentifier(new object(), "field2");
        store1.Add(field1, "Store 1 field 1 message 1");
        store1.Add(field1, "Store 1 field 1 message 2");
        store1.Add(field2, "Store 1 field 2 message 1");
        store2.Add(field1, "Store 2 field 1 message 1");

        // Act/Assert
        Assert.Equal(new[]
        {
                "Store 1 field 1 message 1",
                "Store 1 field 1 message 2",
                "Store 1 field 2 message 1",
                "Store 2 field 1 message 1",
            }, editContext.GetValidationMessages().OrderBy(x => x)); // Sort because the order isn't defined

        // Act/Assert: After clearing a single store, we only see the results from other stores
        store1.Clear();
        Assert.Equal(new[] { "Store 2 field 1 message 1", }, editContext.GetValidationMessages());
    }

    [Fact]
    public void IsValidWithNoValidationMessages()
    {
        // Arrange
        var editContext = new EditContext(new object());

        // Act
        var isValid = editContext.Validate();

        // assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsInvalidWithValidationMessages()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);
        messages.Add(
            new FieldIdentifier(new object(), "some field"),
            "Some message");

        // Act
        var isValid = editContext.Validate();

        // assert
        Assert.False(isValid);
    }

    [Fact]
    public void RequestsValidationWhenValidateIsCalled()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);
        editContext.OnValidationRequested += (sender, eventArgs) =>
        {
            Assert.Same(editContext, sender);
            Assert.NotNull(eventArgs);
            messages.Add(
                new FieldIdentifier(new object(), "some field"),
                "Some message");
        };

        // Act
        var isValid = editContext.Validate();

        // assert
        Assert.False(isValid);
        Assert.Equal(new[] { "Some message" }, editContext.GetValidationMessages());
    }

    [Fact]
    public void IsInvalidWithValidationMessagesForSpecifiedField()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);
        var fieldOnThisModel1 = editContext.Field("field1");
        var fieldOnThisModel2 = editContext.Field("field2");
        messages.Add(
            fieldOnThisModel1,
            "Some message");

        // Assert
        Assert.False(editContext.Validate());
        Assert.False(editContext.IsValid(fieldOnThisModel1));
        Assert.True(editContext.IsValid(fieldOnThisModel2));
    }

    [Fact]
    public void LookingUpModel_ThatOverridesGetHashCodeAndEquals_Works()
    {
        // Test for https://github.com/aspnet/AspNetCore/issues/18069
        // Arrange
        var model = new EquatableModel();
        var editContext = new EditContext(model);

        editContext.NotifyFieldChanged(editContext.Field(nameof(EquatableModel.Property)));

        model.Property = "new value";

        Assert.True(editContext.IsModified(editContext.Field(nameof(EquatableModel.Property))));
    }

    [Fact]
    public void Properties_CanRetrieveViaIndexer()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var key1 = new object();
        var key2 = new object();
        var key3 = new object();
        var value1 = new object();
        var value2 = new object();

        // Initially, the values are not present
        Assert.Throws<KeyNotFoundException>(() => editContext.Properties[key1]);

        // Can store and retrieve values
        editContext.Properties[key1] = value1;
        editContext.Properties[key2] = value2;
        Assert.Same(value1, editContext.Properties[key1]);
        Assert.Same(value2, editContext.Properties[key2]);

        // Unrelated keys are still not found
        Assert.Throws<KeyNotFoundException>(() => editContext.Properties[key3]);
    }

    [Fact]
    public void Properties_CanRetrieveViaTryGetValue()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var key1 = new object();
        var key2 = new object();
        var key3 = new object();
        var value1 = new object();
        var value2 = new object();

        // Initially, the values are not present
        Assert.False(editContext.Properties.TryGetValue(key1, out _));

        // Can store and retrieve values
        editContext.Properties[key1] = value1;
        editContext.Properties[key2] = value2;
        Assert.True(editContext.Properties.TryGetValue(key1, out var retrievedValue1));
        Assert.True(editContext.Properties.TryGetValue(key2, out var retrievedValue2));
        Assert.Same(value1, retrievedValue1);
        Assert.Same(value2, retrievedValue2);

        // Unrelated keys are still not found
        Assert.False(editContext.Properties.TryGetValue(key3, out _));
    }

    [Fact]
    public void Properties_CanRemove()
    {
        // Arrange
        var editContext = new EditContext(new object());
        var key = new object();
        var value = new object();
        editContext.Properties[key] = value;

        // Act
        var resultForExistingKey = editContext.Properties.Remove(key);
        var resultForNonExistingKey = editContext.Properties.Remove(new object());

        // Assert
        Assert.True(resultForExistingKey);
        Assert.False(resultForNonExistingKey);
        Assert.False(editContext.Properties.TryGetValue(key, out _));
        Assert.Throws<KeyNotFoundException>(() => editContext.Properties[key]);
    }

    class EquatableModel : IEquatable<EquatableModel>
    {
        public string Property { get; set; } = "";

        public bool Equals(EquatableModel other)
        {
            return string.Equals(Property, other?.Property, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Property);
        }
    }

    // --- Async validation tests ---

    [Fact]
    public async Task ValidateAsync_InvokesSyncAndAsyncHandlers()
    {
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);

        editContext.OnValidationRequested += (sender, _) =>
        {
            messages.Add(editContext.Field("field1"), "sync error");
        };

        editContext.OnValidationRequestedAsync += async (sender, _) =>
        {
            await Task.Yield();
            messages.Add(editContext.Field("field2"), "async error");
        };

        var isValid = await editContext.ValidateAsync();

        Assert.False(isValid);
        Assert.Equal(new[] { "async error", "sync error" },
            editContext.GetValidationMessages().OrderBy(x => x));
    }

    [Fact]
    public async Task ValidateAsync_ReturnsTrueWhenNoMessages()
    {
        var editContext = new EditContext(new object());
        var syncHandlerCalled = false;
        var asyncHandlerCalled = false;

        editContext.OnValidationRequested += (_, _) => syncHandlerCalled = true;
        editContext.OnValidationRequestedAsync += async (_, _) =>
        {
            await Task.Yield();
            asyncHandlerCalled = true;
        };

        var isValid = await editContext.ValidateAsync();

        Assert.True(isValid);
        Assert.True(syncHandlerCalled);
        Assert.True(asyncHandlerCalled);
    }

    [Fact]
    public async Task ValidateAsync_WorksWithNoHandlers()
    {
        var editContext = new EditContext(new object());

        var isValid = await editContext.ValidateAsync();

        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateAsync_CancelsPendingFieldTasksBeforeRunning()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource();

        editContext.AddValidationTask(field, tcs.Task, cts);
        Assert.True(editContext.IsValidationPending(field));

        await editContext.ValidateAsync();

        Assert.True(cts.IsCancellationRequested);
        Assert.False(editContext.IsValidationPending(field));
    }

    [Fact]
    public async Task AddValidationTask_TracksPendingState()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var otherField = editContext.Field("field2");
        var tcs = new TaskCompletionSource();
        var cts = new CancellationTokenSource();

        Assert.False(editContext.IsValidationPending());
        Assert.False(editContext.IsValidationPending(field));

        editContext.AddValidationTask(field, tcs.Task, cts);

        Assert.True(editContext.IsValidationPending());
        Assert.True(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationPending(otherField));

        tcs.SetResult();
        await tcs.Task;
        // Allow the ObserveValidationTaskAsync continuation to run
        await Task.Yield();

        Assert.False(editContext.IsValidationPending());
        Assert.False(editContext.IsValidationPending(field));
    }

    [Fact]
    public async Task AddValidationTask_CancelsPreviousTaskForSameField()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");

        var cts1 = new CancellationTokenSource();
        var tcs1 = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs1.Task, cts1);

        var cts2 = new CancellationTokenSource();
        var tcs2 = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs2.Task, cts2);

        Assert.True(cts1.IsCancellationRequested);
        Assert.False(cts2.IsCancellationRequested);
        Assert.True(editContext.IsValidationPending(field));

        tcs2.SetResult();
        await Task.Yield();

        Assert.False(editContext.IsValidationPending(field));
    }

    [Fact]
    public async Task AddValidationTask_FaultedTaskSetsFaultedState()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource();

        editContext.AddValidationTask(field, tcs.Task, cts);

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.False(editContext.IsValidationFaulted());

        tcs.SetException(new InvalidOperationException("Network error"));
        await Task.Delay(50); // Allow continuation to run

        Assert.True(editContext.IsValidationFaulted(field));
        Assert.True(editContext.IsValidationFaulted());
        Assert.False(editContext.IsValidationPending(field));
    }

    [Fact]
    public async Task AddValidationTask_CancelledTaskDoesNotFault()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource();

        editContext.AddValidationTask(field, tcs.Task, cts);

        tcs.SetCanceled();
        await Task.Delay(50); // Allow continuation to run

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.False(editContext.IsValidationPending(field));
    }

    [Fact]
    public async Task AddValidationTask_StaleTaskDoesNotOverwriteNewTask()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");

        var cts1 = new CancellationTokenSource();
        var tcs1 = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs1.Task, cts1);

        var cts2 = new CancellationTokenSource();
        var tcs2 = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs2.Task, cts2);

        // Complete the OLD (stale) task — it should not affect state since a newer task replaced it
        tcs1.SetException(new InvalidOperationException("stale failure"));
        await Task.Delay(50);

        // The field should still be pending (tracking the new task), not faulted
        Assert.True(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));

        tcs2.SetResult();
        await Task.Delay(50);

        Assert.False(editContext.IsValidationPending(field));
        Assert.False(editContext.IsValidationFaulted(field));
    }

    [Fact]
    public async Task AddValidationTask_ClearsFaultedStateOnNewTask()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");

        // First task faults
        var cts1 = new CancellationTokenSource();
        var tcs1 = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs1.Task, cts1);
        tcs1.SetException(new InvalidOperationException("fail"));
        await Task.Delay(50);
        Assert.True(editContext.IsValidationFaulted(field));

        // Adding a new task clears faulted state
        var cts2 = new CancellationTokenSource();
        var tcs2 = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs2.Task, cts2);

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.True(editContext.IsValidationPending(field));

        tcs2.SetResult();
        await Task.Yield();
    }

    [Fact]
    public void AddValidationTask_NotifiesValidationStateChanged()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        var tcs = new TaskCompletionSource();
        var cts = new CancellationTokenSource();
        editContext.AddValidationTask(field, tcs.Task, cts);

        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public async Task AddValidationTask_NotifiesValidationStateChangedOnCompletion()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        var tcs = new TaskCompletionSource();
        var cts = new CancellationTokenSource();
        editContext.AddValidationTask(field, tcs.Task, cts);
        Assert.Equal(1, notificationCount); // From AddValidationTask

        tcs.SetResult();
        await Task.Delay(50);

        Assert.Equal(2, notificationCount); // From ObserveValidationTaskAsync completion
    }

    [Fact]
    public void AddValidationTask_RejectsNullArguments()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");

        Assert.Throws<ArgumentNullException>(() =>
            editContext.AddValidationTask(field, null!, new CancellationTokenSource()));

        Assert.Throws<ArgumentNullException>(() =>
            editContext.AddValidationTask(field, Task.CompletedTask, null!));
    }

    [Fact]
    public async Task ValidateAsync_InvokesMultipleAsyncHandlers()
    {
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);
        var handler1Called = false;
        var handler2Called = false;

        editContext.OnValidationRequestedAsync += async (_, _) =>
        {
            await Task.Yield();
            handler1Called = true;
            messages.Add(editContext.Field("f1"), "error1");
        };

        editContext.OnValidationRequestedAsync += async (_, _) =>
        {
            await Task.Yield();
            handler2Called = true;
            messages.Add(editContext.Field("f2"), "error2");
        };

        var isValid = await editContext.ValidateAsync();

        Assert.False(isValid);
        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }
}
