// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Contains the headers values for the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public class HeaderPropagationValues
    {
        private static readonly IReadOnlyDictionary<string, StringValues> _emptyHeaders = new Dictionary<string, StringValues>(capacity: 0);

        private Dictionary<string, StringValues> _headers;

        internal Dictionary<string, StringValues> InputHeaders
        {
            get
            {
                return _headers ??= new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the headers values collected by the <see cref="HeaderPropagationMiddleware"/> from the current request that can be propagated.
        /// </summary>
        public IReadOnlyDictionary<string, StringValues> Headers
        {
            get
            {
                return _headers ?? _emptyHeaders;
            }
        }
    }
}
