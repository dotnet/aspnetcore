// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;
/// <summary>
/// A QuickGrid implementation that uses the same implementation of the basic QuickGrid with the additions of the OnAfterRenderCompleted task.
/// </summary>
/// /// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
internal class NotFailingGrid<TGridItem> : QuickGrid<TGridItem>
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly TaskCompletionSource _onAfterRenderCompleted = new();

    /// <summary>
    /// Task that completes when OnAfterRenderAsync has finished executing.
    /// This allows tests to wait deterministically for the race condition to occur.
    /// </summary>
    public Task OnAfterRenderCompleted => _onAfterRenderCompleted.Task;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                await base.OnAfterRenderAsync(firstRender);
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
