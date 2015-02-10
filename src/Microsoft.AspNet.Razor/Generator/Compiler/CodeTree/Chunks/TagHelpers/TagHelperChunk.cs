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
        /// Instantiates a new <see cref="TagHelperChunk"/>.
        /// </summary>
        /// <param name="tagName">The tag name associated with the tag helpers HTML element.</param>
        /// <param name="selfClosing">
        /// <see cref="bool"/> indicating whether or not the tag of the tag helpers HTML element is self-closing.
        /// </param>
        /// <param name="attributes">The attributes associated with the tag helpers HTML element.</param>
        /// <param name="descriptors">
        /// The <see cref="TagHelperDescriptor"/>s associated with this tag helpers HTML element.
        /// </param>
        public TagHelperChunk(
            string tagName,
            bool selfClosing,
            IDictionary<string, Chunk> attributes,
            IEnumerable<TagHelperDescriptor> descriptors)
        {
            TagName = tagName;
            SelfClosing = selfClosing;
            Attributes = attributes;
            Descriptors = descriptors;
        }

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

        /// <summary>
        /// Gets a value indicating whether or not the tag of the tag helpers HTML element is self-closing.
        /// </summary>
        public bool SelfClosing { get; }
    }
}