// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.AspNetCore.ResponseCaching.Internal;

namespace Microsoft.AspNetCore.Builder
{
    public class ResponseCachingOptions
    {
        /// <summary>
        /// The largest cacheable size for the response body in bytes.
        /// </summary>
        public long MaximumCachedBodySize { get; set; } = 1024 * 1024;

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal ISystemClock SystemClock { get; set; } = new SystemClock();
    }
}
