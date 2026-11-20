// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputFileTest
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task NotifyChange_DispatchesExceptionsToRenderer(bool asynchronous, bool cancellationException)
    {
        using var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var exception = cancellationException ? new OperationCanceledException("Callback failed") : new Exception("Callback failed");
        var completion = new TaskCompletionSource();
        var inputFile = new InputFile
        {
            OnChange = EventCallback.Factory.Create<InputFileChangeEventArgs>(new object(), _ =>
            {
                if (!asynchronous)
                {
                    throw exception;
                }

                return completion.Task;
            }),
        };
        renderer.AssignRootComponentId(inputFile);

        var notification = renderer.Dispatcher.InvokeAsync(() => ((IInputFileJsCallbacks)inputFile).NotifyChange([]));
        if (asynchronous)
        {
            Assert.False(notification.IsCompleted);
            completion.SetException(exception);
        }

        await notification;

        Assert.Same(exception, Assert.Single(renderer.HandledExceptions));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NotifyChange_IgnoresCanceledTasks(bool asynchronous)
    {
        using var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var completion = new TaskCompletionSource();
        var inputFile = new InputFile
        {
            OnChange = EventCallback.Factory.Create<InputFileChangeEventArgs>(new object(), _ =>
                asynchronous ? completion.Task : Task.FromCanceled(new CancellationToken(canceled: true))),
        };
        var componentId = renderer.AssignRootComponentId(inputFile);

        var notification = renderer.Dispatcher.InvokeAsync(() => ((IInputFileJsCallbacks)inputFile).NotifyChange([]));
        if (asynchronous)
        {
            Assert.False(notification.IsCompleted);
            await renderer.Dispatcher.InvokeAsync(() => renderer.RemoveRootComponent(componentId));
            completion.SetCanceled();
        }

        await notification;

        Assert.Empty(renderer.HandledExceptions);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    public async Task NotifyChange_PreservesFilesAndAwaitsCallback(bool asynchronous, int fileCount)
    {
        using var renderer = new TestRenderer();
        var completion = new TaskCompletionSource();
        InputFileChangeEventArgs receivedArgs = null;
        var inputFile = new InputFile
        {
            OnChange = EventCallback.Factory.Create<InputFileChangeEventArgs>(new object(), args =>
            {
                receivedArgs = args;
                return asynchronous ? completion.Task : Task.CompletedTask;
            }),
        };
        renderer.AssignRootComponentId(inputFile);
        var files = Enumerable.Range(0, fileCount).Select(index => new BrowserFile { Name = $"{index}.txt" }).ToArray();

        var notification = renderer.Dispatcher.InvokeAsync(() => ((IInputFileJsCallbacks)inputFile).NotifyChange(files));
        if (asynchronous)
        {
            Assert.False(notification.IsCompleted);
            completion.SetResult();
        }

        await notification;

        Assert.Equal(files, receivedArgs.GetMultipleFiles());
        Assert.All(files, file => Assert.Same(inputFile, file.Owner));
        Assert.Empty(renderer.HandledExceptions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NotifyChange_DispatchesOriginalExceptionAfterDisposal(bool asynchronous)
    {
        using var renderer = new TestRenderer { ShouldHandleExceptions = true };
        var exception = new InvalidOperationException("Callback failed after disposal");
        var completion = new TaskCompletionSource();
        var componentId = -1;
        var inputFile = new InputFile
        {
            OnChange = EventCallback.Factory.Create<InputFileChangeEventArgs>(new object(), _ =>
            {
                if (!asynchronous)
                {
                    renderer.RemoveRootComponent(componentId);
                    throw exception;
                }

                return completion.Task;
            }),
        };
        componentId = renderer.AssignRootComponentId(inputFile);

        var notification = renderer.Dispatcher.InvokeAsync(() => ((IInputFileJsCallbacks)inputFile).NotifyChange([]));
        if (asynchronous)
        {
            Assert.False(notification.IsCompleted);
            await renderer.Dispatcher.InvokeAsync(() => renderer.RemoveRootComponent(componentId));
            completion.SetException(exception);
        }
        await notification;

        Assert.Same(exception, Assert.Single(renderer.HandledExceptions));
    }

    [Fact]
    public void DerivedClass_CanOverrideDisposeMethod()
    {
        // Arrange
        var disposed = false;
        var derivedInputFile = new DerivedInputFile(() => disposed = true);

        // Act
        ((IDisposable)derivedInputFile).Dispose();

        // Assert
        Assert.True(disposed, "Derived class Dispose(bool) method should be called");
    }

    private class DerivedInputFile : InputFile
    {
        private readonly Action _onDispose;

        public DerivedInputFile(Action onDispose)
        {
            _onDispose = onDispose;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _onDispose();
            }
            base.Dispose(disposing);
        }
    }
}
