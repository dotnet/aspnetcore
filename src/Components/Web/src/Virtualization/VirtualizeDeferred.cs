// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides functionality for rendering a virtualized list from a deferred source.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public sealed class VirtualizeDeferred<TItem> : VirtualizeBase<TItem>
    {
        private enum FetchState
        {
            Idle,
            Waiting,
            Busy
        }

        private readonly ConcurrentQueue<TItem> _loadedItems = new ConcurrentQueue<TItem>();

        private FetchState _fetchState;

        private int _itemCount;

        private Task<ItemsProviderResult<TItem>>? _fetchTask;

        private CancellationTokenSource? _fetchTcs;

        /// <summary>
        /// Gets or sets the item template for the list.
        /// </summary>
        [Parameter]
        public RenderFragment<TItem> Item { get; set; } = default!;

        /// <summary>
        /// Gets or sets the template for items that have not yet been loaded in memory.
        /// </summary>
        [Parameter]
        public RenderFragment<int> Placeholder { get; set; } = default!;

        /// <summary>
        /// Gets or sets the function providing items to the list.
        /// </summary>
        [Parameter]
        public ItemsProviderDelegate<TItem> ItemsProvider { get; set; } = default!;

        /// <inheritdoc />
        protected override int ItemCount => _itemCount;

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (Item == null)
            {
                throw new InvalidOperationException($"{GetType()} requires the template parameter '{nameof(Item)}' to be specified.");
            }

            if (Placeholder == null)
            {
                throw new InvalidOperationException($"{GetType()} requires the template parameter '{nameof(Placeholder)}' to be specified.");
            }

            if (ItemsProvider == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a non-null parameter '{nameof(ItemsProvider)}'.");
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (_fetchState == FetchState.Waiting)
            {
                Debug.Assert(_fetchTask != null);

                _fetchState = FetchState.Busy;

                var result = await _fetchTask;

                _itemCount = result.TotalItemCount;

                foreach (var item in result.Items)
                {
                    _loadedItems.Enqueue(item);
                }

                _fetchTcs = null;
                _fetchState = FetchState.Idle;

                StateHasChanged();
            }
        }

        /// <inheritdoc />
        protected override void RenderItems(RenderTreeBuilder builder)
        {
            var itemsRendered = 0;

            foreach (var item in _loadedItems.Skip(ItemsBefore).Take(ItemsVisible))
            {
                Item(item)(builder);
                itemsRendered++;
            }

            for (; itemsRendered < ItemsVisible; itemsRendered++)
            {
                Placeholder(ItemsBefore + itemsRendered)(builder);
            }

            var itemsToFetch = ItemsBefore + VisibleItemCapacity - _loadedItems.Count;

            if (itemsToFetch > 0 && _fetchState == FetchState.Idle)
            {
                _fetchState = FetchState.Waiting;
                _fetchTcs = new CancellationTokenSource();
                _fetchTask = ItemsProvider(_loadedItems.Count, itemsToFetch, _fetchTcs.Token);
            }
        }

        /// <inheritdoc />
        public override ValueTask DisposeAsync()
        {
            _fetchTcs?.Cancel();

            return base.DisposeAsync();
        }
    }
}
