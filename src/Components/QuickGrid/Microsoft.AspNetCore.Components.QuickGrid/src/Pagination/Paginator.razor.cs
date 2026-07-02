// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// A component that provides a user interface for <see cref="PaginationState"/>.
/// </summary>
public partial class Paginator : IDisposable
{
    private readonly EventCallbackSubscriber<PaginationState> _totalItemCountChanged;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    private string QueryName => State.QueryName;

    /// <summary>
    /// Specifies the associated <see cref="PaginationState"/>. This parameter is required.
    /// </summary>
    [Parameter, EditorRequired] public PaginationState State { get; set; } = default!;

    /// <summary>
    /// Optionally supplies a template for rendering the page count summary.
    /// </summary>
    [Parameter] public RenderFragment? SummaryTemplate { get; set; }

    /// <summary>
    /// A callback that is invoked immediately before the page changes.
    /// The callback receives the target page number as a 1-based, user-facing value
    /// (for example, the first page is <c>1</c>, not <c>0</c>).
    /// This is invoked before <see cref="PaginationState.SetCurrentPageIndexAsync(int)"/> is called,
    /// so <see cref="PaginationState.CurrentPageIndex"/> still reflects the previous page when the callback runs.
    /// </summary>
    [Parameter] public EventCallback<int> OnPageChanging { get; set; }

    /// <summary>
    /// A callback that is invoked immediately after the page has changed.
    /// The callback receives the new page number as a 1-based, user-facing value
    /// (for example, the first page is <c>1</c>, not <c>0</c>).
    /// This is invoked after <see cref="PaginationState.SetCurrentPageIndexAsync(int)"/> has completed,
    /// so <see cref="PaginationState.CurrentPageIndex"/> reflects the new page when the callback runs.
    /// </summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>
    /// Constructs an instance of <see cref="Paginator" />.
    /// </summary>
    public Paginator()
    {
        // The "total item count" handler doesn't need to do anything except cause this component to re-render
        _totalItemCountChanged = new(new EventCallback<PaginationState>(this, null));
        _queryParameterValueSupplier = new();
    }

    private readonly QueryParameterValueSupplier _queryParameterValueSupplier;

    private string GetPageUrl(int pageIndex)
    {
        int? pageValue = pageIndex == 0 ? null : pageIndex + 1;
        return NavigationManager.GetUriWithQueryParameter(QueryName, pageValue);
    }

    private Task GoFirstAsync() => GoToPageAsync(0);
    private Task GoPreviousAsync() => GoToPageAsync(State.CurrentPageIndex - 1);
    private Task GoNextAsync() => GoToPageAsync(State.CurrentPageIndex + 1);
    private Task GoLastAsync() => GoToPageAsync(State.LastPageIndex.GetValueOrDefault(0));

    private bool CanGoBack => State.CurrentPageIndex > 0;
    private bool CanGoForwards => State.CurrentPageIndex < State.LastPageIndex;
    private Task GoToPageAsync(int pageIndex)
    {
        NavigationManager.NavigateTo(GetPageUrl(pageIndex));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        _totalItemCountChanged.SubscribeOrMove(State.TotalItemCountChangedSubscribable);

        _queryParameterValueSupplier.ReadParametersFromQuery(QueryParameterValueSupplier.GetQueryString(NavigationManager.Uri));
        var pageFromQuery = ReadPageIndexFromQueryString() ?? 0;
        if (pageFromQuery != State.CurrentPageIndex)
        {
            return HandlePageChangeAsync(pageFromQuery);
        }

        return Task.CompletedTask;
    }

    internal async Task HandlePageChangeAsync(int newPageIndex)
    {
        await OnPageChanging.InvokeAsync(newPageIndex + 1);
        await State.SetCurrentPageIndexAsync(newPageIndex);
        await OnPageChanged.InvokeAsync(newPageIndex + 1);
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _queryParameterValueSupplier.ReadParametersFromQuery(QueryParameterValueSupplier.GetQueryString(NavigationManager.Uri));
        var pageFromQuery = ReadPageIndexFromQueryString() ?? 0;
        await InvokeAsync(async () =>
        {
            if (pageFromQuery != State.CurrentPageIndex)
            {
                await HandlePageChangeAsync(pageFromQuery);
            }
            StateHasChanged();
        });
    }

    private int? ReadPageIndexFromQueryString()
    {
        var value = _queryParameterValueSupplier.GetQueryParameterValue(typeof(string), QueryName) as string;
        if (value is not null && int.TryParse(value, out var page) && page > 0)
        {
            return page - 1;
        }

        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        _totalItemCountChanged.Dispose();
    }
}
