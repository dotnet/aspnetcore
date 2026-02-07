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

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private ITempData? TempData { get; set; }

    private string TempDataKey => $"Paginator_CurrentPageIndex_{Id}";
    private string FormNameFirst => $"Paginator_{Id}_GoFirst";
    private string FormNamePrevious => $"Paginator_{Id}_GoPrevious";
    private string FormNameNext => $"Paginator_{Id}_GoNext";
    private string FormNameLast => $"Paginator_{Id}_GoLast";

    /// <summary>
    /// Gets or sets a unique identifier for this paginator.
    /// Required when using static server-side rendering (SSR) to ensure form names are stable across requests.
    /// </summary>
    [Parameter] public string Id { get; set; } = "Default";

    /// <summary>
    /// Specifies the associated <see cref="PaginationState"/>. This parameter is required.
    /// </summary>
    [Parameter, EditorRequired] public PaginationState State { get; set; } = default!;

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
    private Task GoPreviousAsync() => GoToPageAsync(State.CurrentPageIndex - 1);
    private Task GoNextAsync() => GoToPageAsync(State.CurrentPageIndex + 1);
    private Task GoLastAsync() => GoToPageAsync(State.LastPageIndex.GetValueOrDefault(0));

    private bool CanGoBack => State.CurrentPageIndex > 0;
    private bool CanGoForwards => State.CurrentPageIndex < State.LastPageIndex;

    private async Task GoToPageAsync(int pageIndex)
    {
        TempData?[TempDataKey] = pageIndex;
        await State.SetCurrentPageIndexAsync(pageIndex);
        if (!RendererInfo.IsInteractive)
        {
            // To prevent F5 from resubmitting the form in SSR mode
            NavigationManager.Refresh();
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _totalItemCountChanged.SubscribeOrMove(State.TotalItemCountChangedSubscribable);
        if (TempData?.Get(TempDataKey) is int savedPageIndex)
        {
            State.SetCurrentPageIndexAsync(savedPageIndex);
        }
    }

    /// <inheritdoc />
    public void Dispose()
        => _totalItemCountChanged.Dispose();
}
