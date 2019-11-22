// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// A collection of <see cref="HeaderPropagationMessageHandlerEntry"/> items.
    /// </summary>
    public sealed class HeaderPropagationMessageHandlerEntryCollection : Collection<HeaderPropagationMessageHandlerEntry>
    {
        /// <summary>
        /// Adds an <see cref="HeaderPropagationMessageHandlerEntry"/> that will use <paramref name="headerName"/> as
        /// the value of <see cref="HeaderPropagationMessageHandlerEntry.CapturedHeaderName"/> and
        /// <see cref="HeaderPropagationMessageHandlerEntry.OutboundHeaderName"/>.
        /// </summary>
        /// <param name="headerName">
        /// The name of the header to be added by the <see cref="HeaderPropagationMessageHandler"/>.
        /// </param>
        public void Add(string headerName)
        {
            if (headerName == null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            Add(new HeaderPropagationMessageHandlerEntry(headerName, headerName));
        }

        /// <summary>
        /// Adds an <see cref="HeaderPropagationMessageHandlerEntry"/> that will use the provided <paramref name="capturedHeaderName"/>
        /// and <paramref name="outboundHeaderName"/>.
        /// </summary>
        /// <param name="capturedHeaderName">
        /// The name of the header captured by the <see cref="HeaderPropagationMiddleware"/>.
        /// </param>
        /// <param name="outboundHeaderName">
        /// The name of the header to be added by the <see cref="HeaderPropagationMessageHandler"/>.
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

            Add(new HeaderPropagationMessageHandlerEntry(capturedHeaderName, outboundHeaderName));
        }
    }
}
