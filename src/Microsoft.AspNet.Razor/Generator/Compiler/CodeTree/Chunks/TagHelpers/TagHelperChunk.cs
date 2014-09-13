// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    /// <summary>
    /// A <see cref="ChunkBlock"/> that represents a special HTML tag.
    /// </summary>
    public class TagHelperChunk : ChunkBlock
    {
        /// <summary>
        /// The HTML attributes.
        /// </summary>
        /// <remarks>
        /// These attributes are <see cref="string"/> => <see cref="Chunk"/> so attribute values can consist
        /// of all sorts of Razor specific pieces.
        /// </remarks>
        public IDictionary<string, Chunk> Attributes { get; set; }

        /// <summary>
        /// The <see cref="TagHelperDescriptor"/>s that are associated with the tag helpers HTML element.
        /// </summary>
        public IEnumerable<TagHelperDescriptor> Descriptors { get; set; }

        /// <summary>
        /// The HTML tag name.
        /// </summary>
        public string TagName { get; set; }
    }
}