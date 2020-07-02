// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Represents request header metadata used during routing.
    /// </summary>
    public interface IHeaderMetadata
    {
        /// <summary>
        /// Name of the header to look for.
        /// </summary>
        string HeaderName { get; }

        /// <summary>
        /// Returns a read-only collection of acceptable header values used during routing.
        /// An empty collection means any header value will be accepted, as long as the header is present.
        /// </summary>
        IReadOnlyList<string> HeaderValues { get; }

        /// <summary>
        /// Specifies how header values should be compared (e.g. exact matches Vs. by prefix).
        /// Defaults to <see cref="HeaderValueMatchMode.Exact"/>.
        /// </summary>
        HeaderValueMatchMode ValueMatchMode { get; }

        /// <summary>
        /// Specifies whether header value comparisons should ignore case.
        /// When <c>false</c>, <see cref="StringComparison.Ordinal" /> is used.
        /// When <c>true</c>, <see cref="StringComparison.OrdinalIgnoreCase" /> is used.
        /// Defaults to <c>false</c>.
        /// </summary>
        bool ValueIgnoresCase { get; }

        /// <summary>
        /// Specifies the maximum number of incoming header values to inspect when evaluating each <see cref="HeaderValues"/>.
        /// </summary>
        /// <remarks>
        /// Since header-based routing is commonly used in scenarios where a single header value is expected,
        /// this helps us bail out early for unexpected requests.
        /// </remarks>
        int MaximumValuesToInspect { get; }
    }
}
