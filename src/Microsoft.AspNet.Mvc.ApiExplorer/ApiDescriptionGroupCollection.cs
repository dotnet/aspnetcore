// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ApiExplorer
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
        public ApiDescriptionGroupCollection([NotNull] IReadOnlyList<ApiDescriptionGroup> items, int version)
        {
            Items = items;
            Version = version;
        }

        /// <summary>
        /// Returns the list of <see cref="IReadOnlyList{ApiDescriptionGroup}"/>.
        /// </summary>
        public IReadOnlyList<ApiDescriptionGroup> Items { get; private set; }

        /// <summary>
        /// Returns the unique version of the current items.
        /// </summary>
        public int Version { get; private set; }
    }
}