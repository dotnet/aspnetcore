// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides functionality for rendering a virtualized, infinite list of items.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public sealed class InfiniteScroll<TItem> : VirtualizeBase<TItem>
    {
        private readonly ConcurrentQueue<TItem> _loadedItems = new ConcurrentQueue<TItem>();

        private int _fetchingItems;

        protected override int ItemCount => InfiniteScrollFooter == null ? _loadedItems.Count : _loadedItems.Count + 1;

        /// <summary>
        /// Gets or sets the list item template.
        /// </summary>
        [Parameter]
        public RenderFragment<TItem> Item { get; set; } = default!;

        /// <summary>
        /// Gets or sets the infinite scroll footer template.
        /// </summary>
        [Parameter]
        public RenderFragment? InfiniteScrollFooter { get; set; }

        /// <summary>
        /// Gets or sets the function that provides items for the inifinite scroll.
        /// </summary>
        [Parameter]
        public Func<Range, Task<IEnumerable<TItem>>> ItemsProvider { get; set; } = default!;

        /// <summary>
        /// The batch size in which items are fetched.
        /// </summary>
        [Parameter]
        public int ItemBatchSize { get; set; }

        protected override IEnumerable<RenderFragment> GetItems(int start, int count)
        {
            if (start + count >= _loadedItems.Count)
            {
                // Fetch more items if we're not already
                FetchItemsAsync();

                var items = _loadedItems.Skip(start).Select(item => Item(item));

                return InfiniteScrollFooter == null ? items : items.Append(InfiniteScrollFooter);
            }
            else
            {
                return _loadedItems.Skip(start).Take(count).Select(item => Item(item));
            }
        }

        private void FetchItemsAsync()
        {
            if (Interlocked.Exchange(ref _fetchingItems, 1) == 0)
            {
                ItemsProvider(new Range(_loadedItems.Count, _loadedItems.Count + ItemBatchSize)).ContinueWith(t =>
                {
                    foreach (var item in t.Result)
                    {
                        _loadedItems.Enqueue(item);
                    }

                    _fetchingItems = 0;

                    InvokeAsync(() => StateHasChanged());
                });
            }
        }
    }
}
