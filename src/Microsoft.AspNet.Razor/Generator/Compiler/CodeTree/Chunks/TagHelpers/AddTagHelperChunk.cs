// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    /// <summary>
    /// A <see cref="Chunk"/> used to look up <see cref="TagHelpers.TagHelperDescriptor"/>s.
    /// </summary>
    public class AddTagHelperChunk : Chunk
    {
        /// <summary>
        /// Text used to look up <see cref="TagHelpers.TagHelperDescriptor"/>s.
        /// </summary>
        public string LookupText { get; set; }
    }
}