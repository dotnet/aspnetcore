// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// A collection of <see cref="HeaderPropagationEntry"/> items.
    /// </summary>
    public sealed class HeaderPropagationEntryCollection : Collection<HeaderPropagationEntry>
    {
        /// <summary>
        /// Adds an <see cref="HeaderPropagationEntry"/> that will use <paramref name="headerName"/> as
        /// the value of <see cref="HeaderPropagationEntry.InboundHeaderName"/> and
        /// <see cref="HeaderPropagationEntry.OutboundHeaderName"/>.
        /// </summary>
        /// <param name="headerName">The header name to be propagated.</param>
        public void Add(string headerName)
        {
            if (headerName == null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            Add(new HeaderPropagationEntry(headerName));
        }
    }
}
