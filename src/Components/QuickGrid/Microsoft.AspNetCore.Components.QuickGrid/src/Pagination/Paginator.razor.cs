// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Resources;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// A component that provides a user interface for <see cref="PaginationState"/>.
/// </summary>
public partial class Paginator : IDisposable
{
    private readonly EventCallbackSubscriber<PaginationState> _totalItemCountChanged;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    // IStringLocalizer<Paginator> is an *optional* dependency: QuickGrid is a reusable library that
    // must render even when the consuming app has not called AddLocalization(). Components cannot
    // declare optional [Inject] dependencies, so we resolve it from the service provider ourselves
    // (see ResolveLocalizer) and treat a missing service as "use the embedded fallback".
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private IStringLocalizer<Paginator>? _localizer;
    private bool _localizerResolved;

    private string QueryName => State.QueryName;

    /// <summary>
    /// Embedded resource used when no <see cref="IStringLocalizer{T}"/> is registered or when it
    /// does not contain a requested key. Contains English by default. Created once and shared across
    /// all instances because <see cref="ResourceManager"/> is thread-safe and stateless per lookup.
    /// </summary>
    private static readonly ResourceManager s_fallbackResourceManager = new(
        "Microsoft.AspNetCore.Components.QuickGrid.Resources.QuickGridLocalization",
        typeof(Paginator).Assembly);

    /// <summary>
    /// Localizes a string key. Prefers the app-provided <see cref="IStringLocalizer{T}"/> when it is
    /// registered and actually resolves the key, and otherwise falls back to the embedded resource
    /// for the current UI culture (English by default). The raw key is returned as a last resort so
    /// a missing resource can never render as empty or tear down the renderer.
    /// </summary>
    /// <remarks>
    /// Callers pass only the key; the single formatted string in this component
    /// (<c>PaginationPageStatus</c>) is expanded by <see cref="WritePaginationPageStatus"/> so its
    /// page numbers can be wrapped in <c>&lt;strong&gt;</c> elements rather than string-formatted.
    /// </remarks>
    private string Localize(string key)
    {
        // Prefer the app-provided localizer only when it actually resolved the key. A resource that
        // was "not found" or that resolves to an empty string must not shadow the embedded fallback.
        if (ResolveLocalizer() is { } localizer)
        {
            var value = localizer[key];
            if (!value.ResourceNotFound && !string.IsNullOrEmpty(value.Value))
            {
                return value.Value;
            }
        }

        // Fall back to the embedded resource for the current UI culture, then to the raw key.
        return s_fallbackResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }

    /// <summary>
    /// Resolves the optional <see cref="IStringLocalizer{T}"/> once and caches the result (including a
    /// null result). The lookup is deferred until first use rather than done in <c>OnInitialized</c> so
    /// the component still renders the embedded fallback if no service provider is available.
    /// </summary>
    private IStringLocalizer<Paginator>? ResolveLocalizer()
    {
        if (!_localizerResolved)
        {
            _localizer = Services?.GetService<IStringLocalizer<Paginator>>();
            _localizerResolved = true;
        }

        return _localizer;
    }

    private RenderFragment PaginationPageStatus => builder =>
    {
        var sequence = 0;
        var template = Localize("PaginationPageStatus");

        var currentPage = State.CurrentPageIndex + 1;
        var lastPage = State.LastPageIndex.GetValueOrDefault(0) + 1;

        WritePaginationPageStatus(builder, ref sequence, template, currentPage, lastPage);
    };

    private static void WritePaginationPageStatus(
        RenderTreeBuilder builder,
        ref int sequence,
        string template,
        int currentPage,
        int lastPage)
    {
        var index = 0;

        while (index < template.Length)
        {
            var currentPagePlaceholderIndex = template.IndexOf("{0}", index, StringComparison.Ordinal);
            var lastPagePlaceholderIndex = template.IndexOf("{1}", index, StringComparison.Ordinal);

            if (currentPagePlaceholderIndex == -1 && lastPagePlaceholderIndex == -1)
            {
                builder.AddContent(sequence++, template[index..]);
                return;
            }

            var nextPlaceholderIndex = GetNextPlaceholderIndex(
                currentPagePlaceholderIndex,
                lastPagePlaceholderIndex);

            if (nextPlaceholderIndex > index)
            {
                builder.AddContent(sequence++, template[index..nextPlaceholderIndex]);
            }

            if (nextPlaceholderIndex == currentPagePlaceholderIndex)
            {
                builder.OpenElement(sequence++, "strong");
                builder.AddContent(sequence++, currentPage);
                builder.CloseElement();

                index = currentPagePlaceholderIndex + 3;
            }
            else
            {
                builder.OpenElement(sequence++, "strong");
                builder.AddContent(sequence++, lastPage);
                builder.CloseElement();

                index = lastPagePlaceholderIndex + 3;
            }
        }
    }

    private static int GetNextPlaceholderIndex(
        int currentPagePlaceholderIndex,
        int lastPagePlaceholderIndex)
    {
        if (currentPagePlaceholderIndex == -1)
        {
            return lastPagePlaceholderIndex;
        }

        if (lastPagePlaceholderIndex == -1)
        {
            return currentPagePlaceholderIndex;
        }

        return Math.Min(currentPagePlaceholderIndex, lastPagePlaceholderIndex);
    }

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
            return State.SetCurrentPageIndexAsync(pageFromQuery);
        }

        return Task.CompletedTask;
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _queryParameterValueSupplier.ReadParametersFromQuery(QueryParameterValueSupplier.GetQueryString(NavigationManager.Uri));
        var pageFromQuery = ReadPageIndexFromQueryString() ?? 0;
        await InvokeAsync(async () =>
        {
            if (pageFromQuery != State.CurrentPageIndex)
            {
                await State.SetCurrentPageIndexAsync(pageFromQuery);
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
