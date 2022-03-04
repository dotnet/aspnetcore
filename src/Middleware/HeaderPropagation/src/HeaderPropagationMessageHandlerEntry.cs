// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Define the configuration of an header for the <see cref="HeaderPropagationMessageHandler"/>.
    /// </summary>
    public class HeaderPropagationMessageHandlerEntry
    {
        /// <summary>
        /// Creates a new <see cref="HeaderPropagationMessageHandlerEntry"/> with the provided <paramref name="capturedHeaderName"/>
        /// and <paramref name="outboundHeaderName"/>.
        /// </summary>
        /// <param name="capturedHeaderName">
        /// The name of the header to be used to lookup the headers captured by the <see cref="HeaderPropagationMiddleware"/>.
        /// </param>
        /// <param name="outboundHeaderName">
        /// The name of the header to be added to the outgoing http requests by the <see cref="HeaderPropagationMessageHandler"/>.
        /// </param>
        public HeaderPropagationMessageHandlerEntry(
            string capturedHeaderName,
            string outboundHeaderName)
        {
            if (capturedHeaderName == null)
            {
                throw new ArgumentNullException(nameof(capturedHeaderName));
            }

            if (outboundHeaderName == null)
            {
                throw new ArgumentNullException(nameof(outboundHeaderName));
            }

            CapturedHeaderName = capturedHeaderName;
            OutboundHeaderName = outboundHeaderName;
        }

        /// <summary>
        /// Gets the name of the header to be used to lookup the headers captured by the <see cref="HeaderPropagationMiddleware"/>.
        /// </summary>
        public string CapturedHeaderName { get; }

        /// <summary>
        /// Gets the name of the header to be added to the outgoing http requests by the <see cref="HeaderPropagationMessageHandler"/>.
        /// </summary>
        public string OutboundHeaderName { get; }
    }
}
