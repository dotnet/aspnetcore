// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Contains context for a placeholder in a virtualized list.
    /// </summary>
    public readonly struct PlaceholderContext
    {
        /// <summary>
        /// The item index of the placeholder.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Constructs a new <see cref="PlaceholderContext"/> instance.
        /// </summary>
        /// <param name="index">The item index of the placeholder.</param>
        public PlaceholderContext(int index)
        {
            Index = index;
        }
    }
}
