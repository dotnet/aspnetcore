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
        /// Specifies how header values should be compared as exact matches or by prefix.
        /// Defaults to <see cref="HeaderValueMatchMode.Exact"/>.
        /// </summary>
        HeaderValueMatchMode HeaderValueMatchMode { get; }

        /// <summary>
        /// Specifies string comparison rules, including whether matches are case sensitive or not.
        /// Defaults to <see cref="StringComparison.Ordinal"/>.
        /// </summary>
        StringComparison HeaderValueStringComparison { get; }
    }
}
