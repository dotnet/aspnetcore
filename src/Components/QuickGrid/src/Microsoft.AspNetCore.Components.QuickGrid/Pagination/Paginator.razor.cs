// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// A component that provides a user interface for <see cref="PaginationState"/>.
/// </summary>
public partial class Paginator : IDisposable
{
    private readonly EventCallbackSubscriber<PaginationState> _totalItemCountChanged;

    /// <summary>
    /// Specifies the associated <see cref="PaginationState"/>. This parameter is required.
    /// </summary>
    [Parameter, EditorRequired] public PaginationState Value { get; set; } = default!;

    /// <summary>
    /// Optionally supplies a template for rendering the page count summary.
    /// </summary>
    [Parameter] public RenderFragment? SummaryTemplate { get; set; }

    /// <summary>
    /// Constructs an instance of <see cref="Paginator" />.
    /// </summary>
    public Paginator()
    {
        // The "total item count" handler doesn't need to do anything except cause this component to re-render
        _totalItemCountChanged = new(new EventCallback<PaginationState>(this, null));
    }

    private Task GoFirstAsync() => GoToPageAsync(0);
    private Task GoPreviousAsync() => GoToPageAsync(Value.CurrentPageIndex - 1);
    private Task GoNextAsync() => GoToPageAsync(Value.CurrentPageIndex + 1);
    private Task GoLastAsync() => GoToPageAsync(Value.LastPageIndex.GetValueOrDefault(0));

    private bool CanGoBack => Value.CurrentPageIndex > 0;
    private bool CanGoForwards => Value.CurrentPageIndex < Value.LastPageIndex;

    private Task GoToPageAsync(int pageIndex)
        => Value.SetCurrentPageIndexAsync(pageIndex);

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _totalItemCountChanged.SubscribeOrMove(Value.TotalItemCountChangedSubscribable);

    /// <inheritdoc />
    public void Dispose()
        => _totalItemCountChanged.Dispose();
}
