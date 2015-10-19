// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Chunks
{
    /// <summary>
    /// A <see cref="Chunk"/> used to look up <see cref="Compilation.TagHelpers.TagHelperDescriptor"/>s that should be ignored
    /// within the Razor page.
    /// </summary>
    public class RemoveTagHelperChunk : Chunk
    {
        /// <summary>
        /// Text used to look up <see cref="Compilation.TagHelpers.TagHelperDescriptor"/>s that should be ignored within the Razor
        /// page.
        /// </summary>
        public string LookupText { get; set; }
    }
}