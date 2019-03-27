// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Contains the headers values for the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public class HeaderPropagationValues
    {
        private readonly static AsyncLocal<Dictionary<string, StringValues>> _headers = new AsyncLocal<Dictionary<string, StringValues>>();

        /// <summary>
        /// Gets the headers values collected by the <see cref="HeaderPropagationMiddleware"/> from the current request that can be propagated.
        /// </summary>
        public IDictionary<string, StringValues> Headers
        {
            get
            {
                return _headers.Value ?? (_headers.Value = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}
