// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;

namespace Microsoft.AspNetCore.Routing
{
    public sealed class HeaderMatcherPolicyOptions
    {
        private const int DefaultMaximumRequestHeaderValuesToInspect = 1;

        private int _maximumRequestHeaderValuesToInspect = DefaultMaximumRequestHeaderValuesToInspect;

        /// <summary>
        /// Specifies the maximum number of incoming header values to inspect
        /// when evaluating each <see cref="IHeaderMetadata.HeaderValues"/> for a given endpoint.
        /// </summary>
        /// <remarks>
        /// Since header-based routing is commonly used in scenarios where a single header value is expected,
        /// this enables us to bail out early for unexpected requests.
        /// </remarks>
        public int MaximumRequestHeaderValuesToInspect
        {
            get => _maximumRequestHeaderValuesToInspect;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"${nameof(value)} must be positive.");
                }

                _maximumRequestHeaderValuesToInspect = value;
            }
        }
    }
}
