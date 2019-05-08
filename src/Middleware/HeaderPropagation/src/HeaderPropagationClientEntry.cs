// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Define the configuration of a header for the <see cref="HeaderPropagationMessageHandler"/>.
    /// </summary>
    public class HeaderPropagationClientEntry
    {
        /// <summary>
        /// Creates a new <see cref="HeaderPropagationEntry"/> with the provided <paramref name="capturedHeaderName"/>
        /// and <paramref name="outboundHeaderName"/>
        /// </summary>
        /// <param name="capturedHeaderName">
        /// The default name of the header to be added to clients as configured in the <see cref="HeaderPropagationMiddleware"/>.
        /// </param>
        /// <param name="outboundHeaderName">
        /// The name of the header to be added by <see cref="HeaderPropagationMessageHandler"/>.
        /// </param>
        public HeaderPropagationClientEntry(
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
        /// Gets the name of the header that will be captured by the <see cref="HeaderPropagationMiddleware"/>.
        /// </summary>
        public string CapturedHeaderName { get; }

        /// <summary>
        /// Gets the name of the header to be used by the <see cref="HeaderPropagationMessageHandler"/> for the
        /// outbound http requests.
        /// </summary>
        public string OutboundHeaderName { get; }
    }
}
