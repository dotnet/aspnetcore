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
        /// Header value must match in its entirety, subject to the value of <see cref="IHeaderMetadata.ValueIgnoresCase"/>.
        /// </summary>
        Exact,

        /// <summary>
        /// Header value must match by prefix, subject to the value of <see cref="IHeaderMetadata.ValueIgnoresCase"/>.
        /// </summary>
        Prefix,
    }
}
