// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Contains the outbound header values for the <see cref="HeaderPropagationMessageHandler"/>.
    /// </summary>
    public class HeaderPropagationValues
    {
        private readonly static AsyncLocal<IDictionary<string, StringValues>> _headers = new AsyncLocal<IDictionary<string, StringValues>>();

        /// <summary>
        /// Gets or sets the headers values collected by the <see cref="HeaderPropagationMiddleware"/> from the current request
        /// that can be propagated.
        /// </summary>
        /// <remarks>
        /// The keys of <see cref="Headers"/> correspond to <see cref="HeaderPropagationEntry.OutboundHeaderName"/>.
        /// </remarks>
        public IDictionary<string, StringValues> Headers
        {
            get
            {
                return _headers.Value;
            }
            set
            {
                _headers.Value = value;
            }
        }
    }
}
