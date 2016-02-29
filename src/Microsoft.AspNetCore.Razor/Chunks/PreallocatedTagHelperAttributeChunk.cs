// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Chunks
{
    /// <summary>
    /// A <see cref="Chunk"/> that represents a pre-allocated tag helper attribute.
    /// </summary>
    public class PreallocatedTagHelperAttributeChunk : Chunk
    {
        /// <summary>
        /// The variable holding the pre-allocated attribute.
        /// </summary>
        public string AttributeVariableAccessor { get; set; }
    }
}
