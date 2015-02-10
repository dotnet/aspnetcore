// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Parser.TagHelpers
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
            Attributes = new Dictionary<string, SyntaxTreeNode>(original.Attributes);
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperBlockBuilder"/> class
        /// with the provided <paramref name="tagName"/> and derives its <see cref="Attributes"/>
        /// and <see cref="BlockBuilder.Type"/> from the <paramref name="startTag"/>.
        /// </summary>
        /// <param name="tagName">An HTML tag name.</param>
        /// <param name="selfClosing">
        /// <see cref="bool"/> indicating whether or not the tag in the Razor source was self-closing.
        /// </param>
        /// <param name="start">Starting location of the <see cref="TagHelperBlock"/>.</param>
        /// <param name="attributes">Attributes of the <see cref="TagHelperBlock"/>.</param>
        /// <param name="descriptors">The <see cref="TagHelperDescriptor"/>s associated with the current HTML
        /// tag.</param>
        public TagHelperBlockBuilder(string tagName,
                                     bool selfClosing,
                                     SourceLocation start,
                                     IDictionary<string, SyntaxTreeNode> attributes,
                                     IEnumerable<TagHelperDescriptor> descriptors)
        {
            TagName = tagName;
            SelfClosing = selfClosing;
            Start = start;
            Descriptors = descriptors;
            Attributes = new Dictionary<string, SyntaxTreeNode>(attributes);
            Type = BlockType.Tag;
            CodeGenerator = new TagHelperCodeGenerator(descriptors);
        }

        // Internal for testing
        internal TagHelperBlockBuilder(string tagName,
                                       bool selfClosing,
                                       IDictionary<string, SyntaxTreeNode> attributes,
                                       IEnumerable<SyntaxTreeNode> children)
        {
            TagName = tagName;
            SelfClosing = selfClosing;
            Attributes = attributes;
            Type = BlockType.Tag;
            CodeGenerator = new TagHelperCodeGenerator(tagHelperDescriptors: null);

            // Children is IList, no AddRange
            foreach (var child in children)
            {
                Children.Add(child);
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the tag in the Razor source was self-closing.
        /// </summary>
        public bool SelfClosing { get; }

        /// <summary>
        /// <see cref="TagHelperDescriptor"/>s for the HTML element.
        /// </summary>
        public IEnumerable<TagHelperDescriptor> Descriptors { get; }

        /// <summary>
        /// The HTML attributes.
        /// </summary>
        public IDictionary<string, SyntaxTreeNode> Attributes { get; private set; }

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
        public SourceLocation Start { get; private set; }
    }
}