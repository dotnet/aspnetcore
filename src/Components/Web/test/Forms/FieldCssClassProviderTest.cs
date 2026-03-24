// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components.Web.Forms;

public class FieldCssClassProviderTest
{
    private static readonly FieldCssClassProvider _provider = new();

    [Fact]
    public void ReturnsValid_WhenFieldIsUnmodifiedAndHasNoMessages()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("valid", cssClass);
    }

    [Fact]
    public void ReturnsModifiedValid_WhenFieldIsModifiedAndHasNoMessages()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        editContext.NotifyFieldChanged(field);

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("modified valid", cssClass);
    }

    [Fact]
    public void ReturnsInvalid_WhenFieldHasMessages()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "error");

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("invalid", cssClass);
    }

    [Fact]
    public void ReturnsModifiedInvalid_WhenFieldIsModifiedAndHasMessages()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        editContext.NotifyFieldChanged(field);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "error");

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("modified invalid", cssClass);
    }

    [Fact]
    public void ReturnsPending_WhenFieldHasPendingAsyncValidation()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var tcs = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs.Task, new CancellationTokenSource());

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("pending", cssClass);
    }

    [Fact]
    public void ReturnsModifiedPending_WhenModifiedFieldHasPendingAsyncValidation()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        editContext.NotifyFieldChanged(field);
        var tcs = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs.Task, new CancellationTokenSource());

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("modified pending", cssClass);
    }

    [Fact]
    public async Task ReturnsFaulted_WhenFieldAsyncValidationFaulted()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var tcs = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs.Task, new CancellationTokenSource());

        tcs.SetException(new InvalidOperationException("fail"));
        await Task.Delay(50);

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("faulted", cssClass);
    }

    [Fact]
    public async Task ReturnsModifiedFaulted_WhenModifiedFieldAsyncValidationFaulted()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        editContext.NotifyFieldChanged(field);
        var tcs = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs.Task, new CancellationTokenSource());

        tcs.SetException(new InvalidOperationException("fail"));
        await Task.Delay(50);

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("modified faulted", cssClass);
    }

    [Fact]
    public void PendingTakesPriorityOverValidMessages()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        // Field has no validation messages (would be "valid") but is pending
        var tcs = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs.Task, new CancellationTokenSource());

        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("pending", cssClass);
    }

    [Fact]
    public async Task FaultedTakesPriorityOverValidMessages()
    {
        var editContext = new EditContext(new object());
        var field = editContext.Field("field1");
        var tcs = new TaskCompletionSource();
        editContext.AddValidationTask(field, tcs.Task, new CancellationTokenSource());

        tcs.SetException(new InvalidOperationException("fail"));
        await Task.Delay(50);

        // Field has no validation messages (would be "valid") but is faulted
        var cssClass = _provider.GetFieldCssClass(editContext, field);

        Assert.Equal("faulted", cssClass);
    }
}
