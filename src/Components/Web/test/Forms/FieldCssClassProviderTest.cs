// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class FieldCssClassProviderTest
{
    private readonly FieldCssClassProvider _provider = new();

    [Fact]
    public void ReturnsValid_WhenNotModifiedAndNoMessages()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);

        Assert.Equal("valid", _provider.GetFieldCssClass(editContext, field));
    }

    [Fact]
    public void ReturnsInvalid_WhenNotModifiedWithMessages()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "bad");

        Assert.Equal("invalid", _provider.GetFieldCssClass(editContext, field));
    }

    [Fact]
    public void ReturnsModifiedValid_WhenModifiedAndNoMessages()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        editContext.NotifyFieldChanged(field);

        Assert.Equal("modified valid", _provider.GetFieldCssClass(editContext, field));
    }

    [Fact]
    public void ReturnsModifiedInvalid_WhenModifiedWithMessages()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "bad");
        editContext.NotifyFieldChanged(field);

        Assert.Equal("modified invalid", _provider.GetFieldCssClass(editContext, field));
    }

    [Fact]
    public void ReturnsPending_WhenAsyncTaskInFlight()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.RegisterAsyncFieldValidator(field, _ => tcs.Task);

        Assert.Equal("pending", _provider.GetFieldCssClass(editContext, field));

        tcs.SetResult();
    }

    [Fact]
    public void ReturnsModifiedPending_WhenModifiedAndAsyncTaskInFlight()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        editContext.NotifyFieldChanged(field);
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.RegisterAsyncFieldValidator(field, _ => tcs.Task);

        Assert.Equal("modified pending", _provider.GetFieldCssClass(editContext, field));

        tcs.SetResult();
    }

    [Fact]
    public async Task PendingSupersedesValidityWhenMessagesArePresent()
    {
        // Stale messages from a prior pass don't leak through while we're still validating.
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "stale");
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.RegisterAsyncFieldValidator(field, _ => tcs.Task);

        Assert.Equal("pending", _provider.GetFieldCssClass(editContext, field));

        tcs.SetResult();
        await tcs.Task;
    }

    [Fact]
    public void ReturnsFaulted_WhenLastAsyncValidationFaulted()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        var faultingTask = Task.FromException(new InvalidOperationException("boom"));
        editContext.RegisterAsyncFieldValidator(field, _ => faultingTask);

        Assert.Equal("faulted", _provider.GetFieldCssClass(editContext, field));
    }

    [Fact]
    public void ReturnsModifiedFaulted_WhenModifiedAndLastAsyncValidationFaulted()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        editContext.NotifyFieldChanged(field);
        var faultingTask = Task.FromException(new InvalidOperationException("boom"));
        editContext.RegisterAsyncFieldValidator(field, _ => faultingTask);

        Assert.Equal("modified faulted", _provider.GetFieldCssClass(editContext, field));
    }

    [Fact]
    public void FaultedSupersedesInvalidWhenMessagesArePresent()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var field = FieldIdentifier.Create(() => model.Property);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "ignored-by-faulted");
        var faultingTask = Task.FromException(new InvalidOperationException("boom"));
        editContext.RegisterAsyncFieldValidator(field, _ => faultingTask);

        Assert.Equal("faulted", _provider.GetFieldCssClass(editContext, field));
    }

    private sealed class TestModel
    {
        public string Property { get; set; }
    }
}
