// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>
/// Provides functionality for rendering a virtualized list of items.
/// </summary>
/// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
public sealed class Virtualize<TItem> : ComponentBase, IVirtualizeJsCallbacks, IAsyncDisposable
{
    private VirtualizeJsInterop? _jsInterop;

    private ElementReference _spacerBefore;

    private ElementReference _spacerAfter;

    private int _itemsBefore;

    private int _visibleItemCapacity;

    // If the client reports a viewport so large that it could show more than MaxItemCount items,
    // we keep track of the "unused" capacity, which is the amount of blank space we want to leave
    // at the bottom of the viewport (as a number of items). If we didn't leave this blank space,
    // then the bottom spacer would always stay visible and the client would request more items in an
    // infinite (but asynchronous) loop, as it would believe there are more items to render and
    // enough space to render them into.
    private int _unusedItemCapacity;

    private int _itemCount;

    private int _loadedItemsStartIndex;

    private int _lastRenderedItemCount;

    private int _lastRenderedPlaceholderCount;

    private float _itemSize;

    private IEnumerable<TItem>? _loadedItems;

    private CancellationTokenSource? _refreshCts;

    private Exception? _refreshException;

    private ItemsProviderDelegate<TItem> _itemsProvider = default!;

    private RenderFragment<TItem>? _itemTemplate;

    private RenderFragment<PlaceholderContext>? _placeholder;

    private RenderFragment? _emptyContent;

    private bool _loading;

    private float _totalMeasuredHeight;

    private int _measuredItemCount;

    // Accumulated scroll position compensation for measurement-induced height estimate changes.
    // When ProcessMeasurements updates the running average height, spacer heights shift but scrollTop
    // doesn't adjust (overflow-anchor is disabled). This delta is applied after the DOM renders to
    // keep the user's view position stable.
    private float _pendingScrollDelta;

    // When the user is at the very end of the list and measurements change the height estimate,
    // the total content height shifts. Simple delta compensation doesn't suffice because the
    // rendered content also changes. Re-scrolling to the bottom ensures convergence.
    private bool _pendingScrollToBottom;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the item template for the list.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the item template for the list.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ItemContent { get; set; }

    /// <summary>
    /// Gets or sets the template for items that have not yet been loaded in memory.
    /// </summary>
    [Parameter]
    public RenderFragment<PlaceholderContext>? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the content to show when <see cref="Items"/> is empty
    /// or when the <see cref="ItemsProviderResult&lt;TItem&gt;.TotalItemCount"/> is zero.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// Gets the size of each item in pixels. Defaults to 50px.
    /// </summary>
    [Parameter]
    public float ItemSize { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the function providing items to the list.
    /// </summary>
    [Parameter]
    public ItemsProviderDelegate<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// Gets or sets the fixed item source.
    /// </summary>
    [Parameter]
    public ICollection<TItem>? Items { get; set; }

    /// <summary>
    /// Gets or sets a value that determines how many additional items will be rendered
    /// before and after the visible region. This help to reduce the frequency of rendering
    /// during scrolling. However, higher values mean that more elements will be present
    /// in the page.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 15;

    /// <summary>
    /// Gets or sets the tag name of the HTML element that will be used as the virtualization spacer.
    /// One such element will be rendered before the visible items, and one more after them, using
    /// an explicit "height" style to control the scroll range.
    ///
    /// The default value is "div". If you are placing the <see cref="Virtualize{TItem}"/> instance inside
    /// an element that requires a specific child tag name, consider setting that here. For example when
    /// rendering inside a "tbody", consider setting <see cref="SpacerElement"/> to the value "tr".
    /// </summary>
    [Parameter]
    public string SpacerElement { get; set; } = "div";

    // Matches SpacerElement to maintain valid HTML in tables.
    private string ItemWrapperElement => SpacerElement;

    /// <summary>
    /// Gets or sets the maximum number of items that will be rendered, even if the client reports
    /// that its viewport is large enough to show more. The default value is 100.
    ///
    /// This should only be used as a safeguard against excessive memory usage or large data loads.
    /// Do not set this to a smaller number than you expect to fit on a realistic-sized window, because
    /// that will leave a blank gap below and the user may not be able to see the rest of the content.
    /// </summary>
    [Parameter]
    public int MaxItemCount { get; set; } = 100;

    /// <summary>
    /// Instructs the component to re-request data from its <see cref="ItemsProvider"/>.
    /// This is useful if external data may have changed. There is no need to call this
    /// when using <see cref="Items"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    public async Task RefreshDataAsync()
    {
        // We don't auto-render after this operation because in the typical use case, the
        // host component calls this from one of its lifecycle methods, and will naturally
        // re-render afterwards anyway. It's not desirable to re-render twice.
        await RefreshDataCoreAsync(renderOnSuccess: false);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ItemSize <= 0)
        {
            throw new InvalidOperationException(
                $"{GetType()} requires a positive value for parameter '{nameof(ItemSize)}'.");
        }

        if (_itemSize <= 0)
        {
            _itemSize = ItemSize;
        }

        if (ItemsProvider != null)
        {
            if (Items != null)
            {
                throw new InvalidOperationException(
                    $"{GetType()} can only accept one item source from its parameters. " +
                    $"Do not supply both '{nameof(Items)}' and '{nameof(ItemsProvider)}'.");
            }

            _itemsProvider = ItemsProvider;
        }
        else if (Items != null)
        {
            _itemsProvider = DefaultItemsProvider;

            // When we have a fixed set of in-memory data, it doesn't cost anything to
            // re-query it on each cycle, so do that. This means the developer can add/remove
            // items in the collection and see the UI update without having to call RefreshDataAsync.
            var refreshTask = RefreshDataCoreAsync(renderOnSuccess: false);

            // We know it's synchronous and has its own error handling
            Debug.Assert(refreshTask.IsCompletedSuccessfully);
        }
        else
        {
            throw new InvalidOperationException(
                $"{GetType()} requires either the '{nameof(Items)}' or '{nameof(ItemsProvider)}' parameters to be specified " +
                $"and non-null.");
        }

        _itemTemplate = ItemContent ?? ChildContent;
        _placeholder = Placeholder ?? DefaultPlaceholder;
        _emptyContent = EmptyContent;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsInterop = new VirtualizeJsInterop(this, JSRuntime);
            await _jsInterop.InitializeAsync(_spacerBefore, _spacerAfter);
        }

        // Apply any pending scroll compensation from measurement-induced height changes.
        // This must happen after the DOM renders so the spacer heights are up to date.
        if (_jsInterop != null)
        {
            if (_pendingScrollToBottom)
            {
                _pendingScrollToBottom = false;
                _pendingScrollDelta = 0;
                await _jsInterop.ScrollToBottomAsync();
            }
            else if (MathF.Abs(_pendingScrollDelta) > 0.5f)
            {
                var delta = _pendingScrollDelta;
                _pendingScrollDelta = 0;
                await _jsInterop.AdjustScrollPositionAsync(delta);
            }
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_refreshException != null)
        {
            var oldRefreshException = _refreshException;
            _refreshException = null;

            throw oldRefreshException;
        }

        builder.OpenElement(0, SpacerElement);
        builder.AddAttribute(1, "style", GetSpacerStyle(_itemsBefore));
        builder.AddAttribute(2, "aria-hidden", "true");
        builder.AddElementReferenceCapture(3, elementReference => _spacerBefore = elementReference);
        builder.CloseElement();

        var lastItemIndex = Math.Min(_itemsBefore + _visibleItemCapacity, _itemCount);
        var renderIndex = _itemsBefore;
        var placeholdersBeforeCount = Math.Min(_loadedItemsStartIndex, lastItemIndex);

        builder.OpenRegion(3);

        // Render placeholders before the loaded items.
        for (; renderIndex < placeholdersBeforeCount; renderIndex++)
        {
            // This is a rare case where it's valid for the sequence number to be programmatically incremented.
            // This is only true because we know for certain that no other content will be alongside it.
            builder.AddContent(renderIndex, _placeholder, new PlaceholderContext(renderIndex, _itemSize));
        }

        builder.CloseRegion();

        _lastRenderedItemCount = 0;

        if (_loadedItems != null && !_loading && _itemCount == 0 && _emptyContent != null)
        {
            builder.AddContent(4, _emptyContent);
        }
        else if (_loadedItems != null && _itemTemplate != null)
        {
            var itemsToShow = _loadedItems
                .Skip(_itemsBefore - _loadedItemsStartIndex)
                .Take(lastItemIndex - _loadedItemsStartIndex);

            builder.OpenRegion(5);

            // Render the loaded items, each wrapped in an element for JS measurement.
            foreach (var item in itemsToShow)
            {
                builder.OpenElement(_lastRenderedItemCount, ItemWrapperElement);
                builder.AddAttribute(1, "data-virtualize-item", true);
                builder.SetKey(item);
                _itemTemplate(item)(builder);
                builder.CloseElement();
                _lastRenderedItemCount++;
            }

            renderIndex += _lastRenderedItemCount;

            builder.CloseRegion();
        }

        _lastRenderedPlaceholderCount = Math.Max(0, lastItemIndex - _itemsBefore - _lastRenderedItemCount);

        builder.OpenRegion(6);

        // Render the placeholders after the loaded items.
        for (; renderIndex < lastItemIndex; renderIndex++)
        {
            builder.AddContent(renderIndex, _placeholder, new PlaceholderContext(renderIndex, _itemSize));
        }

        builder.CloseRegion();

        var itemsAfter = Math.Max(0, _itemCount - _visibleItemCapacity - _itemsBefore);

        builder.OpenElement(7, SpacerElement);
        builder.AddAttribute(8, "aria-hidden", "true");
        builder.AddAttribute(9, "style", GetSpacerStyle(itemsAfter, _unusedItemCapacity));
        builder.AddElementReferenceCapture(10, elementReference => _spacerAfter = elementReference);

        builder.CloseElement();
    }

    private string GetSpacerStyle(int itemsInSpacer, int numItemsGapAbove)
    {
        var avgHeight = GetItemHeight();
        return numItemsGapAbove == 0
            ? GetSpacerStyle(itemsInSpacer)
            : $"height: {(itemsInSpacer * avgHeight).ToString(CultureInfo.InvariantCulture)}px; flex-shrink: 0; transform: translateY({(numItemsGapAbove * avgHeight).ToString(CultureInfo.InvariantCulture)}px);";
    }

    private string GetSpacerStyle(int itemsInSpacer)
        => $"height: {(itemsInSpacer * GetItemHeight()).ToString(CultureInfo.InvariantCulture)}px; flex-shrink: 0;";

    private float GetItemHeight()
        => _measuredItemCount > 0 ? _totalMeasuredHeight / _measuredItemCount : _itemSize;

    private bool ProcessMeasurements(float[]? itemHeights)
    {
        if (itemHeights is not { Length: > 0 })
        {
            return false;
        }

        _totalMeasuredHeight += itemHeights.Sum();
        _measuredItemCount += itemHeights.Length;
        return true;
    }

    private void AccumulateScrollCompensation(float oldAvgHeight, float newAvgHeight, int itemsBefore)
    {
        var heightDelta = newAvgHeight - oldAvgHeight;
        if (MathF.Abs(heightDelta) > 0.001f && itemsBefore > 0)
        {
            _pendingScrollDelta += itemsBefore * heightDelta;
        }
    }

    void IVirtualizeJsCallbacks.OnBeforeSpacerVisible(float spacerSize, float spacerSeparation, float containerSize, float[]? itemHeights)
    {
        // Track the height estimate before processing new measurements so we can compute
        // scroll compensation for any measurement-induced shift in spacer heights.
        var oldAvgHeight = GetItemHeight();

        // Process any item measurements from JavaScript
        ProcessMeasurements(itemHeights);

        var newAvgHeight = GetItemHeight();

        CalculateItemDistribution(spacerSize, spacerSeparation, containerSize, out var itemsBefore, out var visibleItemCapacity, out var unusedItemCapacity);

        // Since we know the before spacer is now visible, we absolutely have to slide the window up
        // by at least one element. If we're not doing that, the previous item size info we had must
        // have been wrong, so just move along by one in that case to trigger an update and apply the
        // new size info.
        if (_lastRenderedItemCount > 0 && itemsBefore == _itemsBefore && itemsBefore > 0)
        {
            itemsBefore--;
        }

        // Accumulate scroll compensation for the measurement-induced height change.
        // Only compensate for the per-item height estimate changing, not for _itemsBefore changing
        // (which is a normal scroll-driven window shift). This keeps CanExpandDataSetAndRetainScrollPosition
        // working: dataset count changes don't go through ProcessMeasurements.
        AccumulateScrollCompensation(oldAvgHeight, newAvgHeight, itemsBefore);

        UpdateItemDistribution(itemsBefore, visibleItemCapacity, unusedItemCapacity);
    }

    void IVirtualizeJsCallbacks.OnAfterSpacerVisible(float spacerSize, float spacerSeparation, float containerSize, float[]? itemHeights)
    {
        // Track the height estimate before processing new measurements so we can compute
        // scroll compensation for any measurement-induced shift in spacer heights.
        var oldAvgHeight = GetItemHeight();

        // Process any item measurements from JavaScript
        var hadNewMeasurements = ProcessMeasurements(itemHeights);

        var newAvgHeight = GetItemHeight();

        CalculateItemDistribution(spacerSize, spacerSeparation, containerSize, out var itemsAfter, out var visibleItemCapacity, out var unusedItemCapacity);

        var itemsBefore = Math.Max(0, _itemCount - itemsAfter - visibleItemCapacity);

        // Since we know the after spacer is now visible, we absolutely have to slide the window down
        // by at least one element. If we're not doing that, the previous item size info we had must
        // have been wrong, so just move along by one in that case to trigger an update and apply the
        // new size info.
        if (_lastRenderedItemCount > 0 && itemsBefore == _itemsBefore && itemsBefore < _itemCount - visibleItemCapacity)
        {
            itemsBefore++;
        }

        // When the user is at the very end of the list (no items remain after the viewport) and
        // new items were measured, re-scroll to the actual bottom after the DOM renders.
        // This ensures pressing End converges to item N-1 in a single key press, even
        // when the initial ItemSize estimate is far from the true average. We check for any
        // new measurements (not just threshold-exceeding avg changes) because even tiny avg
        // shifts at the boundary prevent convergence to the true bottom.
        // For mid-list scrolling, use delta compensation instead (keeps position stable without
        // jumping to the bottom). Neither case fires for dataset-count changes
        // (CanExpandDataSetAndRetainScrollPosition) because those don't go through ProcessMeasurements.
        if (itemsAfter == 0 && hadNewMeasurements)
        {
            _pendingScrollToBottom = true;
        }
        else if (MathF.Abs(newAvgHeight - oldAvgHeight) > 0.001f)
        {
            AccumulateScrollCompensation(oldAvgHeight, newAvgHeight, itemsBefore);
        }

        UpdateItemDistribution(itemsBefore, visibleItemCapacity, unusedItemCapacity);
    }

    private void CalculateItemDistribution(
        float spacerSize,
        float spacerSeparation,
        float containerSize,
        out int itemsInSpacer,
        out int visibleItemCapacity,
        out int unusedItemCapacity)
    {
        if (_lastRenderedItemCount > 0)
        {
            _itemSize = (spacerSeparation - (_lastRenderedPlaceholderCount * _itemSize)) / _lastRenderedItemCount;
        }

        if (_itemSize <= 0)
        {
            // At this point, something unusual has occurred, likely due to misuse of this component.
            // Reset the calculated item size to the user-provided item size.
            _itemSize = ItemSize;
        }

        // This AppContext data was added as a stopgap for .NET 8 and earlier, since it was added in a patch
        // where we couldn't add new public API. For backcompat we still support the AppContext setting, but
        // new applications should use the much more convenient MaxItemCount parameter.
        var maxItemCount = AppContext.GetData("Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize.MaxItemCount") switch
        {
            int val => Math.Min(val, MaxItemCount),
            _ => MaxItemCount
        };

        // Count the OverscanCount as used capacity, so we don't end up in a situation where
        // the user has set a very low MaxItemCount and we end up in an infinite loading loop.
        maxItemCount += OverscanCount * 2;

        // Use average measured height for calculations, falling back to _itemSize to avoid division by zero
        var effectiveItemSize = GetItemHeight();
        if (effectiveItemSize <= 0 || float.IsNaN(effectiveItemSize) || float.IsInfinity(effectiveItemSize))
        {
            effectiveItemSize = _itemSize;
        }

        itemsInSpacer = Math.Max(0, (int)Math.Floor(spacerSize / effectiveItemSize) - OverscanCount);
        visibleItemCapacity = (int)Math.Ceiling(containerSize / effectiveItemSize) + 2 * OverscanCount;
        unusedItemCapacity = Math.Max(0, visibleItemCapacity - maxItemCount);
        visibleItemCapacity -= unusedItemCapacity;
    }

    private void UpdateItemDistribution(int itemsBefore, int visibleItemCapacity, int unusedItemCapacity)
    {
        // If the itemcount just changed to a lower number, and we're already scrolled past the end of the new
        // reduced set of items, clamp the scroll position to the new maximum
        if (itemsBefore + visibleItemCapacity > _itemCount)
        {
            itemsBefore = Math.Max(0, _itemCount - visibleItemCapacity);
        }

        // If anything about the offset changed, re-render
        if (itemsBefore != _itemsBefore || visibleItemCapacity != _visibleItemCapacity || unusedItemCapacity != _unusedItemCapacity)
        {
            _itemsBefore = itemsBefore;
            _visibleItemCapacity = visibleItemCapacity;
            _unusedItemCapacity = unusedItemCapacity;
            var refreshTask = RefreshDataCoreAsync(renderOnSuccess: true);

            if (!refreshTask.IsCompleted)
            {
                StateHasChanged();
            }
        }
    }

    private async ValueTask RefreshDataCoreAsync(bool renderOnSuccess)
    {
        _refreshCts?.Cancel();
        CancellationToken cancellationToken;

        if (_itemsProvider == DefaultItemsProvider)
        {
            // If we're using the DefaultItemsProvider (because the developer supplied a fixed
            // Items collection) we know it will complete synchronously, and there's no point
            // instantiating a new CancellationTokenSource
            _refreshCts = null;
            cancellationToken = CancellationToken.None;
        }
        else
        {
            _refreshCts = new CancellationTokenSource();
            cancellationToken = _refreshCts.Token;
            _loading = true;
        }

        var request = new ItemsProviderRequest(_itemsBefore, _visibleItemCapacity, cancellationToken);

        try
        {
            var result = await _itemsProvider(request);

            // Only apply result if the task was not canceled.
            if (!cancellationToken.IsCancellationRequested)
            {
                _itemCount = result.TotalItemCount;
                _loadedItems = result.Items;
                _loadedItemsStartIndex = request.StartIndex;
                _loading = false;

                if (renderOnSuccess)
                {
                    StateHasChanged();
                }
            }
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException oce && oce.CancellationToken == cancellationToken)
            {
                // No-op; we canceled the operation, so it's fine to suppress this exception.
            }
            else
            {
                // Cache this exception so the renderer can throw it.
                _refreshException = e;

                // Re-render the component to throw the exception.
                StateHasChanged();
            }
        }
    }

    private ValueTask<ItemsProviderResult<TItem>> DefaultItemsProvider(ItemsProviderRequest request)
    {
        return ValueTask.FromResult(new ItemsProviderResult<TItem>(
            Items!.Skip(request.StartIndex).Take(request.Count),
            Items!.Count));
    }

    private RenderFragment DefaultPlaceholder(PlaceholderContext context) => (builder) =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"height: {_itemSize.ToString(CultureInfo.InvariantCulture)}px; flex-shrink: 0;");
        builder.CloseElement();
    };

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _refreshCts?.Cancel();

        if (_jsInterop != null)
        {
            await _jsInterop.DisposeAsync();
        }
    }
}
