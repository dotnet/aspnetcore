// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides functionality for rendering a virtualized, fixed-size list of items.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public sealed class Virtualize<TItem> : VirtualizeBase<TItem>
    {
        protected override int ItemCount => Items.Count;

        /// <summary>
        /// Gets or sets the content template for list items.
        /// </summary>
        [Parameter]
        public RenderFragment<TItem>? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the collection of items used to generate the list.
        /// </summary>
        [Parameter]
        public ICollection<TItem> Items { get; set; } = default!;

        protected override IEnumerable<RenderFragment> GetItems(int start, int count)
        {
            return ChildContent == null ?
                Enumerable.Empty<RenderFragment>() :
                Items.Skip(start).Take(count).Select(item => ChildContent.Invoke(item));
        }
    }
}
