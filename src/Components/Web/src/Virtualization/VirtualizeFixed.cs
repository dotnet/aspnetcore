// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides functionality for rendering a virtualized fixed list of items.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public sealed class VirtualizeFixed<TItem> : VirtualizeBase<TItem>
    {
        /// <summary>
        /// Gets or sets the item template for the list.
        /// </summary>
        [Parameter]
        public RenderFragment<TItem>? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the fixed item source.
        /// </summary>
        [Parameter]
        public ICollection<TItem> Items { get; set; } = default!;

        /// <inheritdoc />
        protected override int ItemCount => Items.Count;

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (Items == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a non-null parameter '{nameof(Items)}'.");
            }
        }

        /// <inheritdoc />
        protected override void RenderItems(RenderTreeBuilder builder)
        {
            if (ChildContent != null)
            {
                foreach (var item in Items.Skip(ItemsBefore).Take(ItemsVisible))
                {
                    ChildContent(item)(builder);
                }
            }
        }
    }
}
