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
[CacheBoundaryPolicy(Disallow = true)]
public sealed class Virtualize<TItem> : ComponentBase, IVirtualizeJsCallbacks, IAsyncDisposable
{
    private VirtualizeJsInterop? _jsInterop;

    private ElementReference _spacerBefore;

    private ElementReference _spacerAfter;

    internal int _itemsBefore;

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

    internal int _lastRenderedItemCount;

    internal int _lastRenderedPlaceholderCount;

    private float _itemSize;

    private float _lastSetItemSize;

    private IEnumerable<TItem>? _loadedItems;

    private TItem? _previousFirstLoadedItem;

    private bool _itemComparerExplicitlySet;

    private CancellationTokenSource? _refreshCts;

    private CancellationTokenSource? _currentScrollCts;

    private bool _inFlightScrollHasRendered;

    private TaskCompletionSource? _nextRenderTcs;

    private bool _initialScrollApplied;

    private bool _skipNextDistributionRefresh;

    private Exception? _refreshException;

    private ItemsProviderDelegate<TItem> _itemsProvider = default!;

    private RenderFragment<TItem>? _itemTemplate;

    private RenderFragment<PlaceholderContext>? _placeholder;

    private RenderFragment? _emptyContent;

    private bool _loading;

    internal float _totalMeasuredHeight;

    internal int _measuredItemCount;

    internal bool _pendingScrollToBottom;

    private VirtualizeAnchorMode _lastRenderedAnchorMode;

    // When true, OnAfterRenderAsync tells JS to restore the anchor snapshot
    // so the viewport stays stable after a prepend or append.
    private bool _pendingAnchorRestore;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the item template for the list. See <see cref="ItemContent"/>.
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
    /// Gets or sets the anchor mode that controls how the viewport behaves at the edges
    /// of the list when new items arrive. The default is <see cref="VirtualizeAnchorMode.Start"/>.
    /// </summary>
    [Parameter]
    public VirtualizeAnchorMode AnchorMode { get; set; } = VirtualizeAnchorMode.Start;

    /// <summary>
    /// Gets or sets a comparer used to detect whether items were prepended or appended
    /// when using <see cref="ItemsProvider"/>. The comparer determines if the first loaded
    /// item changed between provider calls, which indicates items were inserted above.
    ///
    /// Defaults to <see cref="EqualityComparer{T}.Default"/>. For records and types implementing
    /// <see cref="IEquatable{T}"/>, the default works automatically (value equality). For classes
    /// without value-equality semantics, provide a comparer that compares by a unique identifier
    /// (e.g., <c>Id</c>); otherwise reference-equality fallback would produce false-positive
    /// prepend detection when the provider returns fresh instances.
    ///
    /// Prepend detection only runs when this parameter is explicitly assigned by the consumer.
    /// The <c>BL0011</c> analyzer warns when <see cref="ItemsProvider"/> is used without an
    /// explicit <see cref="ItemComparer"/> assignment.
    ///
    /// For in-memory <see cref="Items"/>, this parameter is not needed because the component
    /// can detect prepends using object identity.
    /// </summary>
    [Parameter]
    public IEqualityComparer<TItem> ItemComparer
    {
        get => _itemComparer;
        set
        {
            _itemComparer = value;
            _itemComparerExplicitlySet = true;
        }
    }

    /// <summary>
    /// Gets or sets the zero-based index of the item to scroll to on first interactive render.
    /// Applied once when the component first knows its item count and ignored on subsequent re-renders;
    /// to scroll programmatically at any later point, call <see cref="ScrollToItemAsync(int, CancellationToken)"/>.
    /// Out-of-range values are clamped. The default value, <c>0</c>, means no initial scroll.
    /// </summary>
    [Parameter]
    public int InitialItemIndex { get; set; }

    private IEqualityComparer<TItem> _itemComparer = EqualityComparer<TItem>.Default;

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
        _totalMeasuredHeight = 0;
        _measuredItemCount = 0;
        await RefreshDataCoreAsync(renderOnSuccess: false);
    }

    /// <summary>
    /// Scrolls the viewport so the item at <paramref name="itemIndex"/> is aligned to the start of the visible area.
    /// </summary>
    /// <remarks>
    /// Each call cancels any previously-running <see cref="ScrollToItemAsync(int, CancellationToken)"/> (last call wins).
    /// Must be called on the renderer's synchronization context; background-thread callers should wrap with
    /// <see cref="ComponentBase.InvokeAsync(Func{Task})"/> to await completion.
    /// </remarks>
    /// <param name="itemIndex">The zero-based index of the item to scroll to.</param>
    /// <param name="cancellationToken">A token that lets the caller request cancellation.</param>
    /// <returns>A <see cref="Task"/> that completes when the target is aligned or superseded by another call,
    /// or faults with <see cref="OperationCanceledException"/> if <paramref name="cancellationToken"/> is cancelled.</returns>
    public Task ScrollToItemAsync(int itemIndex, CancellationToken cancellationToken = default)
    {
        if (_jsInterop is null)
        {
            // Throw synchronously so misuse is reported on the call site, not on the Task.
            throw new InvalidOperationException(
                $"{nameof(ScrollToItemAsync)} cannot be called before the {nameof(Virtualize<TItem>)} has been initialized for interactive rendering. " +
                $"Use the {nameof(InitialItemIndex)} parameter to set the initial scroll position.");
        }

        return ScrollToItemAsyncCore(itemIndex, cancellationToken);
    }

    private async Task ScrollToItemAsyncCore(int itemIndex, CancellationToken cancellationToken)
    {
        // Cancel-and-switch (last call wins); finally block guards cleanup by ref-equality.
        _currentScrollCts?.Cancel();

        var ourCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _currentScrollCts = ourCts;
        _inFlightScrollHasRendered = false;
        var token = ourCts.Token;

        // Suppress JS spacer-IO callbacks until alignToItem completes or a real user scrolls.
        if (_jsInterop is not null)
        {
            try
            {
                await _jsInterop.BeginProgrammaticScrollAsync();
            }
            catch (OperationCanceledException) { }
            catch (JSDisconnectedException) { }
        }

        try
        {
            token.ThrowIfCancellationRequested();
            var refetchRequired = MoveWindowToContain(itemIndex);
            await EnsureRenderCommittedAsync(refetchRequired, token);
            await AlignToTargetAsync(itemIndex, token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Ignore exceptions caused by cancellations.
        }
        finally
        {
            // Only the current owner clears shared state; orphaned operations leave it alone.
            if (ReferenceEquals(_currentScrollCts, ourCts))
            {
                _currentScrollCts = null;
            }
            ourCts.Dispose();
        }
    }

    private bool MoveWindowToContain(int itemIndex)
    {
        var clamped = ClampToItemRange(itemIndex);
        var capacity = _visibleItemCapacity > 0 ? _visibleItemCapacity : OverscanCount * 2 + 1;
        var desiredItemsBefore = Math.Max(0, clamped - OverscanCount);
        if (_itemCount > 0 && desiredItemsBefore + capacity > _itemCount)
        {
            desiredItemsBefore = Math.Max(0, _itemCount - capacity);
        }

        var windowChanged = desiredItemsBefore != _itemsBefore;
        if (_visibleItemCapacity <= 0)
        {
            // Seed capacity when called before any spacer-observer feedback so RefreshDataCoreAsync asks for a meaningful slice.
            _visibleItemCapacity = capacity;
        }
        if (windowChanged)
        {
            _itemsBefore = desiredItemsBefore;
            _skipNextDistributionRefresh = false;
        }

        var alreadyLoadedForWindow = _loadedItems is not null && _loadedItemsStartIndex == _itemsBefore;
        return windowChanged || !alreadyLoadedForWindow;
    }

    private async Task EnsureRenderCommittedAsync(bool refetchRequired, CancellationToken token)
    {
        // Set up the signal BEFORE any render: on WASM, OnAfterRenderAsync runs synchronously inside RefreshDataCoreAsync.
        var renderCommitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _nextRenderTcs = renderCommitTcs;
        using var renderReg = token.Register(static state => ((TaskCompletionSource)state!).TrySetCanceled(), renderCommitTcs);

        if (refetchRequired)
        {
            await RefreshDataCoreAsync(renderOnSuccess: true);
            token.ThrowIfCancellationRequested();
        }
        else
        {
            // Window already loaded — trigger one render so we can wait for the DOM to reflect it.
            StateHasChanged();
        }

        // OnAfterRenderAsync signals once the rendered slice matches our window, so one await is enough.
        await renderCommitTcs.Task;
        token.ThrowIfCancellationRequested();
    }

    private async ValueTask AlignToTargetAsync(int itemIndex, CancellationToken token)
    {
        // Re-clamp in case _itemCount shifted during the fetch.
        var localIndex = ClampToItemRange(itemIndex) - _itemsBefore;
        if (localIndex < 0 || localIndex >= _visibleItemCapacity || _lastRenderedItemCount == 0)
        {
            // Window doesn't contain the target (e.g., empty provider result) — bail cleanly.
            return;
        }

        // Pixel-exact one-shot scroll: JS reads getBoundingClientRect() and sets scrollTop.
        if (_jsInterop is not null)
        {
            await _jsInterop.AlignToItemAsync(localIndex, token);
        }
    }

    private int ClampToItemRange(int requested)
    {
        if (_itemCount <= 0)
        {
            return Math.Max(0, requested);
        }
        if (requested < 0)
        {
            return 0;
        }
        if (requested >= _itemCount)
        {
            return _itemCount - 1;
        }
        return requested;
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

        // Without this reset, visibleItemCapacity is under/over-estimated after a size change,
        // causing extra provider calls that may never complete (e.g., async providers).
        if (_lastSetItemSize != ItemSize)
        {
            _lastSetItemSize = ItemSize;
            _totalMeasuredHeight = 0;
            _measuredItemCount = 0;
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

        // Pre-position the window at InitialItemIndex before the first render so the initial
        // ItemsProvider fetch targets the right slice and avoids a flash of item 0.
        if (!_initialScrollApplied && InitialItemIndex > 0)
        {
            MoveWindowToContain(InitialItemIndex);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Wait until the loaded slice matches the window; earlier renders measure stale DOM.
        var pendingRenderTcs = _nextRenderTcs;
        if (pendingRenderTcs is not null && _loadedItemsStartIndex == _itemsBefore && (_lastRenderedItemCount > 0 || _itemCount == 0))
        {
            _nextRenderTcs = null;
            _inFlightScrollHasRendered = true;
            pendingRenderTcs.TrySetResult();
        }

        if (firstRender)
        {
            _jsInterop = new VirtualizeJsInterop(this, JSRuntime);
            await _jsInterop.InitializeAsync(_spacerBefore, _spacerAfter, (int)AnchorMode);
            _lastRenderedAnchorMode = AnchorMode;
        }

        if (_pendingScrollToBottom && _jsInterop is not null)
        {
            _pendingScrollToBottom = false;
            await _jsInterop.ScrollToBottomAsync();
        }

        // After render the set of items could change. Tell JS to refresh ResizeObserver.
        if (!firstRender && _jsInterop is not null)
        {
            if (_lastRenderedAnchorMode != AnchorMode)
            {
                _lastRenderedAnchorMode = AnchorMode;
                await _jsInterop.SetAnchorModeAsync((int)AnchorMode);
            }

            // If a mutation captured an anchor snapshot, restore it to keep the same row at the
            // same viewport offset. Skip while a ScrollToItemAsync is in flight — we are
            // intentionally moving the viewport.
            var shouldRestore = _pendingAnchorRestore && !_pendingScrollToBottom && _currentScrollCts is null;
            _pendingAnchorRestore = false;

            if (shouldRestore)
            {
                await _jsInterop.RestoreAnchorAsync();
            }

            await _jsInterop.RefreshObserversAsync();
        }

        // Apply InitialItemIndex once: drive the first fetch via ScrollToItemAsync rather than
        // letting the spacer-IO callback fire at scrollTop=0 and reset the window to index 0.
        if (!_initialScrollApplied && _jsInterop is not null)
        {
            if (InitialItemIndex > 0)
            {
                _initialScrollApplied = true;
                await ScrollToItemAsync(InitialItemIndex);
            }
            else if (_itemCount > 0)
            {
                _initialScrollApplied = true;
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
        builder.AddAttribute(1, "data-blazor-virtualize-reserved-height", GetSpacerHeightPx(_itemsBefore));
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

            var isFirstRenderedItem = true;
            foreach (var item in itemsToShow)
            {
                _itemTemplate(item)(builder);
                _lastRenderedItemCount++;

                if (isFirstRenderedItem && _itemComparerExplicitlySet && _itemsProvider != DefaultItemsProvider)
                {
                    _previousFirstLoadedItem = item;
                    isFirstRenderedItem = false;
                }
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
        builder.AddAttribute(9, "data-blazor-virtualize-reserved-height", GetSpacerHeightPx(itemsAfter));
        if (_unusedItemCapacity != 0)
        {
            builder.AddAttribute(10, "data-blazor-virtualize-loop-breaker-transform", GetSpacerHeightPx(_unusedItemCapacity));
        }
        builder.AddElementReferenceCapture(11, elementReference => _spacerAfter = elementReference);

        builder.CloseElement();
    }

    private string GetSpacerHeightPx(int itemCount)
        => (itemCount * GetItemHeight()).ToString(CultureInfo.InvariantCulture);

    private float GetItemHeight()
        => _measuredItemCount > 0 ? _totalMeasuredHeight / _measuredItemCount : _itemSize;

    private bool ProcessMeasurements(float spacerSeparation)
    {
        // Accumulate item height measurements only when no placeholders are rendered,
        // so spacerSeparation directly represents real item heights. This avoids a
        // feedback loop: subtracting (placeholderCount * _itemSize) makes the accumulated
        // measurements depend on _itemSize, which itself depends on those measurements.
        // Under CSS zoom, rounding errors in that loop compound and diverge from reality.
        if (_lastRenderedItemCount <= 0 || _lastRenderedPlaceholderCount > 0)
        {
            return false;
        }

        if (spacerSeparation > 0)
        {
            _totalMeasuredHeight += spacerSeparation;
            _measuredItemCount += _lastRenderedItemCount;
            return true;
        }

        return false;
    }

    private bool ShouldSuppressSpacerCallback()
    {
        // Before the initial ScrollToItemAsync runs, ignore IO callbacks: at scrollTop=0
        // they would compute itemsBefore=0 and overwrite the pre-positioned window.
        if (!_initialScrollApplied && InitialItemIndex > 0)
        {
            return true;
        }

        if (_currentScrollCts is null)
        {
            return false;
        }
        if (_inFlightScrollHasRendered)
        {
            // After our render commits, IO callbacks reflect the alignToItem-driven scrollTop change — suppress them.
            return true;
        }
        // Before our render commits, IO callbacks reflect a real user scroll: cancel the
        // programmatic scroll and the in-flight provider call so the user's window wins.
        _currentScrollCts.Cancel();
        _currentScrollCts = null;
        _refreshCts?.Cancel();
        return false;
    }

    void IVirtualizeJsCallbacks.OnBeforeSpacerVisible(float spacerSize, float spacerSeparation, float containerSize)
    {
        if (_pendingAnchorRestore || ShouldSuppressSpacerCallback())
        {
            return;
        }

        ProcessMeasurements(spacerSeparation);

        CalculateItemDistribution(spacerSize, spacerSeparation, containerSize, out var itemsBefore, out var visibleItemCapacity, out var unusedItemCapacity);

        // Slide window up by at least one if spacer is visible but position unchanged.
        if (_lastRenderedItemCount > 0 && itemsBefore == _itemsBefore && itemsBefore > 0)
        {
            itemsBefore--;
        }

        UpdateItemDistribution(itemsBefore, visibleItemCapacity, unusedItemCapacity);
    }

    void IVirtualizeJsCallbacks.OnAfterSpacerVisible(float spacerSize, float spacerSeparation, float containerSize)
    {
        if (_pendingAnchorRestore || ShouldSuppressSpacerCallback())
        {
            return;
        }

        var hadNewMeasurements = ProcessMeasurements(spacerSeparation);

        CalculateItemDistribution(spacerSize, spacerSeparation, containerSize, out var itemsAfter, out var visibleItemCapacity, out var unusedItemCapacity);

        var itemsBefore = Math.Max(0, _itemCount - itemsAfter - visibleItemCapacity);

        // Slide window down by at least one if spacer is visible but position unchanged.
        if (_lastRenderedItemCount > 0 && itemsBefore == _itemsBefore && itemsBefore < _itemCount - visibleItemCapacity)
        {
            itemsBefore++;
        }

        // Track whether the viewport is at the bottom of the list.
        // In End mode, keep scrolling to bottom while measurements converge.
        if (itemsAfter == 0 && hadNewMeasurements)
        {
            if ((AnchorMode & VirtualizeAnchorMode.End) != 0)
            {
                _pendingScrollToBottom = true;
                _pendingAnchorRestore = false;
            }
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

            // After a successful data load, the ResizeObserver→IntersectionObserver cycle
            // re-triggers with refined measurements. This one-shot flag skips the single
            // redundant provider call that follows. At end-of-list, don't skip: refined
            // capacity may reveal that more items are needed to fill the viewport.
            var skipRefresh = _skipNextDistributionRefresh
                && _loadedItems != null
                && _loadedItemsStartIndex == _itemsBefore
                && _itemsBefore + visibleItemCapacity < _itemCount;
            _skipNextDistributionRefresh = false;

            if (skipRefresh)
            {
                StateHasChanged();
            }
            else
            {
                var refreshTask = RefreshDataCoreAsync(renderOnSuccess: true);

                if (!refreshTask.IsCompleted)
                {
                    StateHasChanged();
                }
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

            // InitialItemIndex out-of-range or TotalItemCount shrank between fetches: re-clamp
            if (!cancellationToken.IsCancellationRequested
                && result.TotalItemCount > 0
                && _itemsBefore >= result.TotalItemCount)
            {
                _itemCount = result.TotalItemCount;
                MoveWindowToContain(_itemsBefore);
                request = new ItemsProviderRequest(_itemsBefore, _visibleItemCapacity, cancellationToken);
                result = await _itemsProvider(request);
            }

            // Only apply result if the task was not canceled.
            if (!cancellationToken.IsCancellationRequested)
            {
                var previousItemCount = _itemCount;
                var countDelta = result.TotalItemCount - previousItemCount;
                var itemsAdded = countDelta > 0 && previousItemCount > 0;
                var isDefaultProvider = _itemsProvider == DefaultItemsProvider;

                if (itemsAdded && isDefaultProvider && _previousFirstLoadedItem != null)
                {
                    var newFirstItem = Items!.ElementAtOrDefault(_itemsBefore);
                    // Use EqualityComparer<TItem>.Default so this works for value-type TItem;
                    // ReferenceEquals would always return false due to boxing.
                    if (newFirstItem != null && !EqualityComparer<TItem>.Default.Equals(_previousFirstLoadedItem, newFirstItem))
                    {
                        result = await AdjustForPrependAsync(countDelta, result.TotalItemCount, cancellationToken);
                    }
                    else if (ShouldAnchorForAppend(countDelta, previousItemCount))
                    {
                        _pendingAnchorRestore = true;
                    }
                    else if (ShouldScrollToBottomForAppend(countDelta, previousItemCount))
                    {
                        _pendingScrollToBottom = true;
                    }
                }
                else if (itemsAdded && !isDefaultProvider && _itemComparerExplicitlySet && _previousFirstLoadedItem != null)
                {
                    using var enumerator = result.Items.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        var itemsShifted = !ItemComparer.Equals(_previousFirstLoadedItem, enumerator.Current);

                        if (itemsShifted)
                        {
                            result = await AdjustForPrependAsync(countDelta, result.TotalItemCount, cancellationToken);
                        }
                        else if (ShouldAnchorForAppend(countDelta, previousItemCount))
                        {
                            _pendingAnchorRestore = true;
                        }
                        else if (ShouldScrollToBottomForAppend(countDelta, previousItemCount))
                        {
                            _pendingScrollToBottom = true;
                        }
                    }
                }

                _itemCount = result.TotalItemCount;
                _loadedItems = result.Items;
                _loadedItemsStartIndex = _itemsBefore;

                // For DefaultItemsProvider, capture the first loaded item so we can detect
                // prepends via EqualityComparer<TItem>.Default (works for both reference and
                // value types — see comment on the comparison above).
                // For custom providers, _previousFirstLoadedItem is set during BuildRenderTree
                // (using the actual rendered item for ItemComparer).
                if (_itemsProvider == DefaultItemsProvider)
                {
                    _previousFirstLoadedItem = Items != null && _itemsBefore < Items.Count
                        ? Items.ElementAtOrDefault(_itemsBefore)
                        : default;
                }

                _loading = false;
                _skipNextDistributionRefresh = request.Count > 0;

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

                // Surface the exception to any waiting ScrollToItemAsync caller.
                _nextRenderTcs?.TrySetException(e);

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
        builder.AddAttribute(1, "data-blazor-virtualize-reserved-height", GetSpacerHeightPx(1));
        builder.CloseElement();
    };

    private async ValueTask<ItemsProviderResult<TItem>> AdjustForPrependAsync(
        int countDelta, int newTotalCount, CancellationToken cancellationToken)
    {
        _itemsBefore = Math.Min(_itemsBefore + countDelta, Math.Max(0, newTotalCount - _visibleItemCapacity));
        _pendingAnchorRestore = true;

        var adjustedRequest = new ItemsProviderRequest(_itemsBefore, _visibleItemCapacity, cancellationToken);
        return await _itemsProvider(adjustedRequest);
    }

    // Items appended at the bottom while viewport is near the end.
    // In non-End modes, restore the anchor so the viewport doesn't
    // chase the new items via spacer redistribution.
    private bool ShouldAnchorForAppend(int countDelta, int previousItemCount)
        => countDelta > 0
            && (AnchorMode & VirtualizeAnchorMode.End) == 0
            && _itemsBefore + _visibleItemCapacity >= previousItemCount;

    private bool ShouldScrollToBottomForAppend(int countDelta, int previousItemCount)
        => countDelta > 0
            && (AnchorMode & VirtualizeAnchorMode.End) != 0
            && previousItemCount <= _visibleItemCapacity;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _refreshCts?.Cancel();

        _currentScrollCts?.Cancel();
        _nextRenderTcs?.TrySetCanceled(CancellationToken.None);

        if (_jsInterop != null)
        {
            await _jsInterop.DisposeAsync();
        }
    }
}
