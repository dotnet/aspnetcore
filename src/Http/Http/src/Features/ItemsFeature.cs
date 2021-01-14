// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IItemsFeature"/>.
    /// </summary>
    public class ItemsFeature : IItemsFeature
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ItemsFeature"/>.
        /// </summary>
        public ItemsFeature()
        {
            Items = new ItemsDictionary();
        }

        /// <inheritdoc />
        public IDictionary<object, object?> Items { get; set; }
    }
}
