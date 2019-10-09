// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// A cached collection of <see cref="ApiDescriptionGroup" />.
    /// </summary>
    public class ApiDescriptionGroupCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDescriptionGroupCollection"/>.
        /// </summary>
        /// <param name="items">The list of <see cref="ApiDescriptionGroup"/>.</param>
        /// <param name="version">The unique version of discovered groups.</param>
        public ApiDescriptionGroupCollection(IReadOnlyList<ApiDescriptionGroup> items, int version)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            Items = items;
            Version = version;
        }

        /// <summary>
        /// Returns the list of <see cref="IReadOnlyList{ApiDescriptionGroup}"/>.
        /// </summary>
        public IReadOnlyList<ApiDescriptionGroup> Items { get; }

        /// <summary>
        /// Returns the unique version of the current items.
        /// </summary>
        public int Version { get; }
    }
}