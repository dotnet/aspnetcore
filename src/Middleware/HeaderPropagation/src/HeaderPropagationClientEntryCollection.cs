// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// A collection of <see cref="HeaderPropagationEntry"/> items.
    /// </summary>
    public sealed class HeaderPropagationClientEntryCollection : Collection<HeaderPropagationClientEntry>
    {
        /// <summary>
        /// Adds an <see cref="HeaderPropagationEntry"/> that will use <paramref name="headerName"/> as
        /// the value of <see cref="HeaderPropagationEntry.InboundHeaderName"/> and
        /// <see cref="HeaderPropagationEntry.CapturedHeaderName"/>.
        /// </summary>
        /// <param name="headerName">The header name to be propagated.</param>
        public void Add(string headerName)
        {
            if (headerName == null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            Add(new HeaderPropagationClientEntry(headerName, headerName));
        }

        /// <summary>
        /// Adds an <see cref="HeaderPropagationEntry"/> that will use the provided <paramref name="capturedHeaderName"/>
        /// and <paramref name="outboundHeaderName"/>.
        /// </summary>
        /// <param name="capturedHeaderName">
        /// The name of the header to be captured by <see cref="HeaderPropagationMiddleware"/>.
        /// </param>
        /// <param name="outboundHeaderName">
        /// The name of the header to be added by <see cref="HeaderPropagationMessageHandler"/>.
        /// </param>
        public void Add(string capturedHeaderName, string outboundHeaderName)
        {
            if (capturedHeaderName == null)
            {
                throw new ArgumentNullException(nameof(capturedHeaderName));
            }

            if (outboundHeaderName == null)
            {
                throw new ArgumentNullException(nameof(outboundHeaderName));
            }

            Add(new HeaderPropagationClientEntry(capturedHeaderName, outboundHeaderName));
        }
    }
}
