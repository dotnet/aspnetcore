// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Define the configuration of a header for the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public class HeaderPropagationEntry
    {
        /// <summary>
        /// Creates a new <see cref="HeaderPropagationEntry"/> with the provided <paramref name="inboundHeaderName"/>.
        /// </summary>
        /// <param name="inboundHeaderName">
        /// The name of the header to be captured by <see cref="HeaderPropagationMiddleware"/> and added by
        /// <see cref="HeaderPropagationMessageHandler"/>.
        /// </param>
        public HeaderPropagationEntry(string inboundHeaderName)
        {
            if (inboundHeaderName == null)
            {
                throw new ArgumentNullException(nameof(inboundHeaderName));
            }

            InboundHeaderName = inboundHeaderName;
            OutboundHeaderName = inboundHeaderName;
        }

        /// <summary>
        /// Creates a new <see cref="HeaderPropagationEntry"/> with the provided <paramref name="inboundHeaderName"/>
        /// and <paramref name="outboundHeaderName"/>
        /// </summary>
        /// <param name="inboundHeaderName">
        /// The name of the header to be captured by <see cref="HeaderPropagationMiddleware"/>.
        /// </param>
        ///  <param name="outboundHeaderName">
        /// The name of the header to be added by <see cref="HeaderPropagationMessageHandler"/>.
        /// </param>
        public HeaderPropagationEntry(string inboundHeaderName, string outboundHeaderName)
        {
            if (inboundHeaderName == null)
            {
                throw new ArgumentNullException(nameof(inboundHeaderName));
            }

            if (outboundHeaderName == null)
            {
                throw new ArgumentNullException(nameof(outboundHeaderName));
            }

            InboundHeaderName = inboundHeaderName;
            OutboundHeaderName = outboundHeaderName;
        }

        /// <summary>
        /// Gets the name of the header that will be captured by the <see cref="HeaderPropagationMiddleware"/>.
        /// </summary>
        public string InboundHeaderName { get; }

        /// <summary>
        /// Gets the name of the header to be used by the <see cref="HeaderPropagationMessageHandler"/> for the
        /// outbound http requests.
        /// </summary>
        public string OutboundHeaderName { get; }

        /// <summary>
        /// Gets or sets a filter delegate that can be used to transform the header value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When present, the delegate will be evaluated once per request to provide the transformed
        /// header value. The delegate will be called regardless of whether a header with the name
        /// corresponding to <see cref="InboundHeaderName"/> is present in the request. If the result
        /// of evaluating <see cref="ValueFilter"/> is null or empty, it will not be added to the propagated
        /// values.
        /// </para>
        /// </remarks>
        public Func<HeaderPropagationContext, StringValues> ValueFilter { get; set; }
    }
}
