// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Concurrent;
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
    public class VirtualizeDeferred<TItem> : VirtualizeBase<TItem>
    {
        private readonly ConcurrentQueue<TItem> _loadedItems = new ConcurrentQueue<TItem>();

        private int _isFetchingItems;

        private int _itemCount;

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
                Placeholder(ItemsBefore + ItemsVisible + itemsRendered)(builder);
            }

            var itemsToFetch = ItemsBefore + VisibleItemCapacity - _loadedItems.Count;

            if (itemsToFetch > 0)
            {
                _ = FetchItemsAsync(itemsToFetch);
            }
        }

        private async Task FetchItemsAsync(int count)
        {
            if (Interlocked.Exchange(ref _isFetchingItems, 1) == 0)
            {
                // TODO: Handle exceptions from the items provider
                var result = await ItemsProvider(_loadedItems.Count, count);

                _itemCount = result.TotalItemCount;

                foreach (var item in result.Items)
                {
                    _loadedItems.Enqueue(item);
                }

                _isFetchingItems = 0;

                StateHasChanged();
            }
        }
    }
}
