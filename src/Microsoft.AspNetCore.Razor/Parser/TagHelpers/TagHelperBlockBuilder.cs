// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Parser.TagHelpers
{
    /// <summary>
    /// A <see cref="BlockBuilder"/> used to create <see cref="TagHelperBlock"/>s.
    /// </summary>
    public class TagHelperBlockBuilder : BlockBuilder
    {
        /// <summary>
        /// Instantiates a new <see cref="TagHelperBlockBuilder"/> instance based on the given
        /// <paramref name="original"/>.
        /// </summary>
        /// <param name="original">The original <see cref="TagHelperBlock"/> to copy data from.</param>
        public TagHelperBlockBuilder(TagHelperBlock original)
            : base(original)
        {
            TagName = original.TagName;
            Descriptors = original.Descriptors;
            Attributes = new List<TagHelperAttributeNode>(original.Attributes);
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperBlockBuilder"/> class
        /// with the provided values.
        /// </summary>
        /// <param name="tagName">An HTML tag name.</param>
        /// <param name="tagMode">HTML syntax of the element in the Razor source.</param>
        /// <param name="start">Starting location of the <see cref="TagHelperBlock"/>.</param>
        /// <param name="attributes">Attributes of the <see cref="TagHelperBlock"/>.</param>
        /// <param name="descriptors">The <see cref="TagHelperDescriptor"/>s associated with the current HTML
        /// tag.</param>
        public TagHelperBlockBuilder(
            string tagName,
            TagMode tagMode,
            SourceLocation start,
            IList<TagHelperAttributeNode> attributes,
            IEnumerable<TagHelperDescriptor> descriptors)
        {
            TagName = tagName;
            TagMode = tagMode;
            Start = start;
            Descriptors = descriptors;
            Attributes = new List<TagHelperAttributeNode>(attributes);
            Type = BlockType.Tag;
            ChunkGenerator = new TagHelperChunkGenerator(descriptors);
        }

        // Internal for testing
        internal TagHelperBlockBuilder(
            string tagName,
            TagMode tagMode,
            IList<TagHelperAttributeNode> attributes,
            IEnumerable<SyntaxTreeNode> children)
        {
            TagName = tagName;
            TagMode = tagMode;
            Attributes = attributes;
            Type = BlockType.Tag;
            ChunkGenerator = new TagHelperChunkGenerator(tagHelperDescriptors: null);

            // Children is IList, no AddRange
            foreach (var child in children)
            {
                Children.Add(child);
            }
        }

        /// <summary>
        /// Gets or sets the unrewritten source start tag.
        /// </summary>
        /// <remarks>This is used by design time to properly format <see cref="TagHelperBlock"/>s.</remarks>
        public Block SourceStartTag { get; set; }

        /// <summary>
        /// Gets or sets the unrewritten source end tag.
        /// </summary>
        /// <remarks>This is used by design time to properly format <see cref="TagHelperBlock"/>s.</remarks>
        public Block SourceEndTag { get; set; }

        /// <summary>
        /// Gets the HTML syntax of the element in the Razor source.
        /// </summary>
        public TagMode TagMode { get; }

        /// <summary>
        /// <see cref="TagHelperDescriptor"/> bindings for the HTML element.
        /// </summary>
        public IEnumerable<TagHelperDescriptor> Descriptors { get; }

        /// <summary>
        /// The HTML attributes.
        /// </summary>
        public IList<TagHelperAttributeNode> Attributes { get; }

        /// <summary>
        /// The HTML tag name.
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// Constructs a new <see cref="TagHelperBlock"/>.
        /// </summary>
        /// <returns>A <see cref="TagHelperBlock"/>.</returns>
        public override Block Build()
        {
            return new TagHelperBlock(this);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Sets the <see cref="TagName"/> to <c>null</c> and clears the <see cref="Attributes"/>.
        /// </remarks>
        public override void Reset()
        {
            TagName = null;

            if (Attributes != null)
            {
                Attributes.Clear();
            }

            base.Reset();
        }

        /// <summary>
        /// The starting <see cref="SourceLocation"/> of the tag helper.
        /// </summary>
        public SourceLocation Start { get; set; }
    }
}