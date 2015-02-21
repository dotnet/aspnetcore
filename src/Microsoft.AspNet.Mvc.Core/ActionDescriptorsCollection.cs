// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A cached collection of <see cref="ActionDescriptor" />.
    /// </summary>
    public class ActionDescriptorsCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionDescriptorsCollection"/>.
        /// </summary>
        /// <param name="items">The result of action discovery</param>
        /// <param name="version">The unique version of discovered actions.</param>
        public ActionDescriptorsCollection([NotNull] IReadOnlyList<ActionDescriptor> items, int version)
        {
            Items = items;
            Version = version;
        }

        /// <summary>
        /// Returns the cached <see cref="IReadOnlyList{ActionDescriptor}"/>.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> Items { get; private set; }

        /// <summary>
        /// Returns the unique version of the currently cached items.
        /// </summary>
        public int Version { get; private set; }
    }
}