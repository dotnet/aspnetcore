// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.JSInterop;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

/// <summary>
/// A QuickGrid implementation that simulates the behavior before the race condition fix.
/// This class intentionally does NOT set _disposeBool during disposal to simulate the race condition.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
internal class FailingQuickGrid<TGridItem> : QuickGrid<TGridItem>, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly TaskCompletionSource _onAfterRenderCompleted = new();

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
    public bool IsWasDisposedFalse()
    {
        var field = typeof(QuickGrid<TGridItem>).GetField("_wasDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
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
                if (JS != null)
                {
                    // Import the JS module (this will trigger our TestJsRuntime's import logic)
                    var jsModule = await JS.InvokeAsync<IJSObjectReference>("import",
                        "./_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js");
                    await jsModule.InvokeAsync<IJSObjectReference>("init", new object());
                }
            }
        }
        finally
        {
            if (firstRender)
            {
                _onAfterRenderCompleted.TrySetResult();
            }
        }
    }
}
