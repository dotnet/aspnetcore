// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides functionality for rendering a virtualized list of items.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public sealed class Virtualize<TItem> : ComponentBase, IVirtualizeJsCallbacks, IAsyncDisposable
    {
        private readonly List<TItem> _loadedItems = new List<TItem>();

        private VirtualizeJsInterop? _jsInterop;

        private ElementReference _spacerBefore;

        private ElementReference _spacerAfter;

        private int _itemsBefore;

        private int _visibleItemCapacity;

        private int _itemsVisible;

        private int _itemCount;

        private Task _fetchTask = Task.CompletedTask;

        private CancellationTokenSource? _fetchCts;

        private ItemsProviderDelegate<TItem> _itemsProvider = default!;

        private RenderFragment<TItem>? _itemTemplate;

        private RenderFragment<int>? _placeholder;

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
        public RenderFragment<TItem>? Item { get; set; }

        /// <summary>
        /// Gets or sets the template for items that have not yet been loaded in memory.
        /// </summary>
        [Parameter]
        public RenderFragment<int>? Placeholder { get; set; }

        /// <summary>
        /// Gets the size of each item in pixels.
        /// </summary>
        [Parameter]
        public float ItemSize { get; set; }

        /// <summary>
        /// Gets or sets the function providing items to the list.
        /// </summary>
        [Parameter]
        public ItemsProviderDelegate<TItem> ItemsProvider { get; set; } = default!;

        /// <summary>
        /// Gets or sets the fixed item source.
        /// </summary>
        [Parameter]
        public ICollection<TItem> Items { get; set; } = default!;

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (ItemSize <= 0)
            {
                throw new InvalidOperationException(
                    $"{GetType()} requires a positive value for parameter '{nameof(ItemSize)}' to perform virtualization.");
            }

            if (ItemsProvider != null)
            {
                if (Items != null)
                {
                    throw new InvalidOperationException($"{GetType()} can only accept one item source from its parameters.");
                }

                _itemsProvider = ItemsProvider;
            }
            else if (Items != null)
            {
                _itemsProvider = DefaultItemsProvider;
            }
            else
            {
                throw new InvalidOperationException(
                    $"{GetType()} requires either the '{nameof(Items)}' or '{nameof(ItemsProvider)}' parameters to be specified " +
                    $"and non-null.");
            }

            _itemTemplate = Item ?? ChildContent;
            _placeholder = Placeholder ?? DefaultPlaceholder;
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _jsInterop = new VirtualizeJsInterop(this, JSRuntime);
                await _jsInterop.InitAsync(_spacerBefore, _spacerAfter);
            }

            if (_fetchTask != null && !_fetchTask.IsCompletedSuccessfully)
            {
                try
                {
                    await _fetchTask;

                    StateHasChanged();
                }
                catch (OperationCanceledException)
                {
                    // No-op.
                }
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var itemsAfter = Math.Max(0, _itemCount - _itemsVisible - _itemsBefore);

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", GetSpacerStyle(_itemsBefore));
            builder.AddElementReferenceCapture(2, elementReference => _spacerBefore = elementReference);
            builder.CloseElement();

            builder.OpenRegion(3);

            var itemsRendered = 0;

            // Render as many items as will visibly fit.
            foreach (var item in _loadedItems.Skip(_itemsBefore).Take(_itemsVisible))
            {
                builder.AddContent(0, _itemTemplate, item);
                itemsRendered++;
            }

            // Render placholder items to fill the remaining space.
            for (; itemsRendered < _itemsVisible; itemsRendered++)
            {
                builder.AddContent(0, _placeholder, _itemsBefore + itemsRendered);
            }

            builder.CloseRegion();

            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "style", GetSpacerStyle(itemsAfter));
            builder.AddElementReferenceCapture(6, elementReference => _spacerAfter = elementReference);

            builder.CloseElement();
        }

        private string GetSpacerStyle(int itemsInSpacer)
            => $"height: {itemsInSpacer * ItemSize}px;";

        void IVirtualizeJsCallbacks.OnBeforeSpacerVisible(float spacerSize, float containerSize)
        {
            _itemsBefore = CalcualteItemDistribution(spacerSize, containerSize);

            StateHasChanged();
        }

        void IVirtualizeJsCallbacks.OnBottomSpacerVisible(float spacerSize, float containerSize)
        {
            var itemsAfter = CalcualteItemDistribution(spacerSize, containerSize);

            _itemsBefore = Math.Max(0, _itemCount - itemsAfter - _itemsVisible);

            if (_itemsBefore + _visibleItemCapacity > _loadedItems.Count)
            {
                _fetchTask = FetchItems();
            }

            StateHasChanged();
        }

        private int CalcualteItemDistribution(float spacerSize, float containerSize)
        {
            _visibleItemCapacity = (int)Math.Ceiling(containerSize / ItemSize) + 2;
            _itemsVisible = Math.Clamp(_itemCount, 0, _visibleItemCapacity);

            return Math.Max(0, (int)Math.Floor(spacerSize / ItemSize) - 1);
        }

        private async Task FetchItems()
        {
            // Cancel the previous fetch, if it exists.
            _fetchCts?.Cancel();

            // Wait for the task to complete. If it fails due to an exception, the exception will bubble-up
            // through `OnAfterRenderAsync`.
            if (!_fetchTask.IsCanceled)
            {
                await _fetchTask;
            }

            var placeholderItems = _itemsBefore + _visibleItemCapacity - _loadedItems.Count;

            // There's a chance that no more items need to be fetched after the previous task completes.
            if (placeholderItems <= 0)
            {
                return;
            }

            _fetchCts = new CancellationTokenSource();

            var result = await _itemsProvider(new ItemsProviderRequest(_loadedItems.Count, placeholderItems, _fetchCts.Token));

            _itemCount = result.TotalItemCount;
            _loadedItems.AddRange(result.Items);
        }

        private Task<ItemsProviderResult<TItem>> DefaultItemsProvider(ItemsProviderRequest request)
        {
            return Task.FromResult(new ItemsProviderResult<TItem>(
                Items.Skip(request.StartIndex).Take(request.Count).ToList(),
                Items.Count));
        }

        private RenderFragment DefaultPlaceholder(int index) => (builder) =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", $"height: {ItemSize}px !important;");
            builder.SetKey(GetHashCode() ^ index);
            builder.CloseElement();
        };

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            _fetchCts?.Cancel();

            if (_jsInterop != null)
            {
                await _jsInterop.DisposeAsync();
            }
        }
    }
}
