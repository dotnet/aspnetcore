// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.JSInterop;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.QuickGrid.Test;

/// <summary>
/// A QuickGrid implementation that simulates the behavior before the race condition fix.
/// This class intentionally does NOT set _disposeBool during disposal to simulate the race condition.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
internal class FailingQuickGrid<TGridItem> : QuickGrid<TGridItem>, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly TaskCompletionSource _onAfterRenderCompleted = new();
    private bool _completionSignaled;

    public bool DisposeAsyncWasCalled { get; private set; }

    /// <summary>
    /// Task that completes when OnAfterRenderAsync has finished executing.
    /// This allows tests to wait deterministically for the race condition to occur.
    /// </summary>
    public Task OnAfterRenderCompleted => _onAfterRenderCompleted.Task;

    /// <summary>
    /// Intentionally does NOT call base.DisposeAsync() to prevent _disposeBool from being set.
    /// This simulates the behavior before the fix was implemented.
    /// </summary>
    public new async ValueTask DisposeAsync()
    {
        DisposeAsyncWasCalled = true;
        // Intentionally do nothing to prevent _disposeBool from being set to true
        // This means the OnAfterRenderAsync method will not detect that the component is disposed
        // and will proceed to call init() even after disposal, demonstrating the race condition

        // DO NOT call base.DisposeAsync() - this is the key to simulating the race condition
        await Task.CompletedTask;
    }

    /// <summary>
    /// Explicit interface implementation to ensure our disposal method is called.
    /// </summary>
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }

    /// <summary>
    /// Check if _disposeBool is false, proving we didn't call base.DisposeAsync().
    /// This is used by tests to verify that our simulation is working correctly.
    /// </summary>
    public bool IsDisposeBoolFalse()
    {
        var field = typeof(QuickGrid<TGridItem>).GetField("_disposeBool", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(this) is false;
    }

    /// <summary>
    /// Override OnAfterRenderAsync to simulate the race condition by NOT checking _disposeBool.
    /// This exactly replicates the code path that existed before the race condition fix.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                // Get the IJSRuntime (same as base class)
                if (JS != null)
                {
                    // Import the JS module (this will trigger our TestJsRuntime's import logic)
                    var jsModule = await JS.InvokeAsync<IJSObjectReference>("import",
                        "./_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js");

                    // THE KEY DIFFERENCE: The original code did NOT check _disposeBool here
                    // The fix added: if (_disposeBool) return;
                    // By omitting this check, we demonstrate the race condition where init gets called on disposed components

                    // Call init - this happens even if component was disposed during import
                    // For our test, we don't need a real table reference, just need to trigger the JS call
                    await jsModule.InvokeAsync<IJSObjectReference>("init", new object());

                    // Signal completion only after the init call has completed, and only once
                    if (!_completionSignaled)
                    {
                        _completionSignaled = true;
                        _onAfterRenderCompleted.TrySetResult();
                    }
                    return;
                }
            }
        }
        finally
        {
            // Only signal completion if we haven't already done it and this is the first render
            if (firstRender && !_completionSignaled)
            {
                _completionSignaled = true;
                _onAfterRenderCompleted.TrySetResult();
            }
        }
    }
}