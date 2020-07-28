// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides functionality for rendering a virtualized list from a source of unknown size.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public sealed class InfiniteScroll<TItem> : ComponentBase
    {
        /// <summary>
        /// Gets or sets the item template for the list.
        /// </summary>
        [Parameter]
        public RenderFragment<TItem> Item { get; set; } = default!;

        /// <summary>
        /// Gets or sets the template for the footer of the list.
        /// </summary>
        [Parameter]
        public RenderFragment<int> Footer { get; set; } = default!;

        /// <summary>
        /// Gets or sets the function retrieving items given a start index.
        /// </summary>
        [Parameter]
        public ItemsProviderDelegate<TItem> ItemsProvider { get; set; } = default!;

        /// <summary>
        /// Gets or sets the item count requested to the <see cref="ItemsProvider"/>.
        /// </summary>
        [Parameter]
        public int RequestedItemCount { get; set; } = 50;

        /// <summary>
        /// Gets the size of each item in pixels.
        /// </summary>
        [Parameter]
        public float ItemSize { get; set; }

        private int _itemCount;

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (Item == null)
            {
                throw new InvalidOperationException(
                    $"{GetType()} requires template parameter '{nameof(Item)}' to be specified and non-null.");
            }

            if (Footer == null)
            {
                throw new InvalidOperationException(
                    $"{GetType()} requires template parameter '{nameof(Footer)}' to be specified and non-null.");
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<VirtualizeDeferred<TItem>>(0);
            builder.AddAttribute(1, "Item", Item);
            builder.AddAttribute(2, "Placeholder", Footer);
            builder.AddAttribute(3, "ItemsProvider", (ItemsProviderDelegate<TItem>)FetchItems);
            builder.AddAttribute(4, "ItemSize", ItemSize);
            builder.CloseComponent();
        }

        private async Task<ItemsProviderResult<TItem>> FetchItems(int start, int count, CancellationToken cancellationToken)
        {
            var result = await ItemsProvider(start, RequestedItemCount, cancellationToken);

            var items = new List<TItem>(result.Items);

            _itemCount += items.Count;

            return new ItemsProviderResult<TItem>(items, _itemCount + 1);
        }
    }
}
