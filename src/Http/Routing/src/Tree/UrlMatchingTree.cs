// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Tree
{
    /// <summary>
    /// A tree part of a <see cref="TreeRouter"/>.
    /// </summary>
    public class UrlMatchingTree
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UrlMatchingTree"/>.
        /// </summary>
        /// <param name="order">The order associated with routes in this <see cref="UrlMatchingTree"/>.</param>
        public UrlMatchingTree(int order)
        {
            Order = order;
        }

        /// <summary>
        /// Gets the order of the routes associated with this <see cref="UrlMatchingTree"/>.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Gets the root of the <see cref="UrlMatchingTree"/>.
        /// </summary>
        public UrlMatchingNode Root { get; } = new UrlMatchingNode(length: 0);
    }
}
