// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides common functionality for virtualized lists.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public abstract class VirtualizeBase<TItem> : ComponentBase
    {
        private readonly ConcurrentQueue<TItem> _loadedItems = new ConcurrentQueue<TItem>();

        private int _isFetchingItems;

        /// <summary>
        /// Gets an enumerable of all list items in memory.
        /// </summary>
        protected IEnumerable<TItem> ItemSource { get; private set; } = default!;

        /// <summary>
        /// Gets the template for rendering list items.
        /// </summary>
        protected RenderFragment<TItem>? ItemTemplate { get; private set; }

        /// <summary>
        /// Gets whether the list is virtualized.
        /// </summary>
        protected bool IsVirtualized => ItemSize > 0f;

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
        /// Gets or sets the template of the footer to be rendered at the end of the list.
        /// </summary>
        [Parameter]
        public RenderFragment? Footer { get; set; }

        /// <summary>
        /// Gets the size of each item in pixels.
        /// </summary>
        [Parameter]
        public float ItemSize { get; set; }

        /// <summary>
        /// Gets or sets the fixed item source.
        /// </summary>
        [Parameter]
        public ICollection<TItem>? Items { get; set; }

        /// <summary>
        /// Gets or sets the function retrieving items given a start index.
        /// </summary>
        [Parameter]
        public Func<int, Task<IEnumerable<TItem>>>? ItemsProvider { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (Items != null)
            {
                if (ItemsProvider != null)
                {
                    throw new InvalidOperationException($"{GetType()} cannot accept multiple item sources.");
                }

                ItemSource = Items;
            }
            else if (ItemsProvider != null)
            {
                ItemSource = _loadedItems;
            }
            else
            {
                throw new InvalidOperationException($"{GetType()} requires either '{nameof(Items)}' or '{nameof(ItemsProvider)}' to be specified and non-null.");
            }

            ItemTemplate = Item ?? ChildContent;
        }

        /// <summary>
        /// Fetches items using <see cref="ItemsProvider"/>.
        /// If <see cref="ItemsProvider"/> is <c>null</c> or items are already being fetched, this method
        /// performs nothing.
        /// </summary>
        /// <returns>The <see cref="Task"/> representing the completion of this operation.</returns>
        protected async Task FetchItemsAsync()
        {
            if (ItemsProvider == null)
            {
                return;
            }

            if (Interlocked.Exchange(ref _isFetchingItems, 1) == 0)
            {
                // TODO: Handle exceptions from the items provider
                var items = await ItemsProvider(_loadedItems.Count);

                foreach (var item in items)
                {
                    _loadedItems.Enqueue(item);
                }

                _isFetchingItems = 0;

                StateHasChanged();
            }
        }
    }
}
