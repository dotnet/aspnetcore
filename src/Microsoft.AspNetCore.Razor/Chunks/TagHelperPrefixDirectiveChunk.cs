// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Chunks
{
    /// <summary>
    /// A <see cref="Chunk"/> for the <c>@tagHelperPrefix</c> directive.
    /// </summary>
    public class TagHelperPrefixDirectiveChunk : Chunk
    {
        /// <summary>
        /// Text used as a required prefix when matching HTML start and end tags in the Razor source to available 
        /// tag helpers.
        /// </summary>
        public string Prefix { get; set; }
    }
}