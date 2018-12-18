// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Hosting
{
    /// <summary>
    /// Identifies a compiled item that can be identified and loaded. 
    /// </summary>
    public abstract class RazorCompiledItem
    {
        /// <summary>
        /// Gets the identifier associated with the compiled item. The identifier is used programmatically to locate
        /// a specific item of a specific kind and should be uniqure within the assembly.
        /// </summary>
        public abstract string Identifier { get; }

        /// <summary>
        /// Gets the kind of compiled item. The kind is used programmatically to associate behaviors and semantics
        /// with the item.
        /// </summary>
        public abstract string Kind { get; }

        /// <summary>
        /// Gets a collection of arbitrary metadata associated with the item.
        /// </summary>
        /// <remarks>
        /// For items loaded with the default implementation of <see cref="RazorCompiledItemLoader"/>, the 
        /// metadata collection will return all attributes defined on the <see cref="Type"/>.
        /// </remarks>
        public abstract IReadOnlyList<object> Metadata { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the compiled item.
        /// </summary>
        public abstract Type Type { get; }
    }
}
