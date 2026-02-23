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
    private bool _hasReadQueryString;
    private bool _suppressNextLocationChange;
    private int _lastRenderedPageIndex;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    private string QueryName => State.QueryName;
    private string FormNameFirst => $"Paginator_{QueryName}_GoFirst";
    private string FormNamePrevious => $"Paginator_{QueryName}_GoPrevious";
    private string FormNameNext => $"Paginator_{QueryName}_GoNext";
    private string FormNameLast => $"Paginator_{QueryName}_GoLast";

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
        int? pageValue = pageIndex == 0 ? null : pageIndex + 1;
        var newUri = NavigationManager.GetUriWithQueryParameter(QueryName, pageValue);
        await State.SetCurrentPageIndexAsync(pageIndex);
        _lastRenderedPageIndex = State.CurrentPageIndex;
        _suppressNextLocationChange = true;
        NavigationManager.NavigateTo(newUri);
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
        if (!_hasReadQueryString)
        {
            _hasReadQueryString = true;
            var pageFromQuery = ReadPageIndexFromQueryString() ?? 0;
            _lastRenderedPageIndex = pageFromQuery;
            if (pageFromQuery != State.CurrentPageIndex)
            {
                return State.SetCurrentPageIndexAsync(pageFromQuery);
            }
        }
        return Task.CompletedTask;
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_suppressNextLocationChange)
        {
            _suppressNextLocationChange = false;
            return;
        }
        var pageFromQuery = ReadPageIndexFromQueryString() ?? 0;
        if (pageFromQuery != _lastRenderedPageIndex)
        {
            _lastRenderedPageIndex = pageFromQuery;
            await InvokeAsync(async () =>
            {
                if (pageFromQuery != State.CurrentPageIndex)
                {
                    await State.SetCurrentPageIndexAsync(pageFromQuery);
                }
                StateHasChanged();
            });
        }
    }

    private int? ReadPageIndexFromQueryString()
    {
        var value = QueryStringHelper.ReadQueryStringValue(NavigationManager.Uri, QueryName);
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
