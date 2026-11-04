// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;

// One awkwardness of the way QuickGrid collects its list of child columns is that, during OnParametersSetAsync,
// it only knows about the set of columns that were present on the *previous* render. If it's going to trigger a
// data load during OnParametersSetAsync, that operation can't depend on the current set of columns as it might
// have changed, or might even still be empty (i.e., on the first render).
//
// Ways this could be resolved:
//
// - In the future, we could implement the long-wanted feature of being able to query the contents of a RenderFragment
//   separately from rendering. Then the whole trick of collection-during-rendering would not be needed.
// - Or, we could factor out most of QuickGrid's internals into some new component QuickGridCore. The parent component,
//   QuickGrid, would then only be responsible for collecting columns followed by rendering QuickGridCore. So each time
//   QuickGridCore renders, we'd already have the latest set of columns
//    - Drawback: since QuickGrid has public API, it's much messier to have to forward all of that to some new child type.
//    - However, this is arguably the most correct solution in general (at least until option 1 above is implemented)
// - Or, we could decide it's enough to fix this on the first render (since that's the only time we're going to guarantee
//   to apply a default sort order), and then as a special case put in some extra component in the render flow that raises
//   an event once the columns are first collected.
//    - This is relatively simple and non-disruptive, though it doesn't cover cases where queries need to be delayed until
//      after a dynamically-added column is added
//
// The final option is what's implemented here. We send the notification via EventCallbackSubscribable so that the async
// operation and re-rendering follows normal semantics without us having to call StateHasChanged or think about exceptions.

/// <summary>
/// For internal use only. Do not use.
/// </summary>
/// <typeparam name="TGridItem">For internal use only. Do not use.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ColumnsCollectedNotifier<TGridItem> : IComponent
{
    private bool _isFirstRender = true;

    [CascadingParameter] internal InternalGridContext<TGridItem> InternalGridContext { get; set; } = default!;

    /// <inheritdoc/>
    public void Attach(RenderHandle renderHandle)
    {
        // This component never renders, so we can ignore the renderHandle
    }

    /// <inheritdoc/>
    public Task SetParametersAsync(ParameterView parameters)
    {
        if (_isFirstRender)
        {
            _isFirstRender = false;
            parameters.SetParameterProperties(this);
            return InternalGridContext.ColumnsFirstCollected.InvokeCallbacksAsync(null);
        }
        else
        {
            return Task.CompletedTask;
        }
    }
}
