// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// How to compare header values.
    /// </summary>
    public enum HeaderValueMatchMode
    {
        /// <summary>
        /// Header value must match exactly using <see cref="string.Equals(string, string, StringComparison)"/>,
        /// subject to the value of <see cref="IHeaderMetadata.HeaderValueStringComparison"/>
        /// </summary>
        Exact,

        /// <summary>
        /// Header value must match one of the provided prefixes
        /// using <see cref="string.StartsWith(string, StringComparison)"/>,
        /// subject to the value of <see cref="IHeaderMetadata.HeaderValueStringComparison"/>
        /// </summary>
        Prefix,
    }
}
