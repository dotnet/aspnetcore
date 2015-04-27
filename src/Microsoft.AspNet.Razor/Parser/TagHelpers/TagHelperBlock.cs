// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Parser.TagHelpers
{
    /// <summary>
    /// A <see cref="Block"/> that reprents a special HTML element.
    /// </summary>
    public class TagHelperBlock : Block, IEquatable<TagHelperBlock>
    {
        private readonly SourceLocation _start;

        /// <summary>
        /// Instantiates a new instance of a <see cref="TagHelperBlock"/>.
        /// </summary>
        /// <param name="source">A <see cref="TagHelperBlockBuilder"/> used to construct a valid
        /// <see cref="TagHelperBlock"/>.</param>
        public TagHelperBlock(TagHelperBlockBuilder source)
            : base(source.Type, source.Children, source.CodeGenerator)
        {
            TagName = source.TagName;
            Descriptors = source.Descriptors;
            Attributes = new List<KeyValuePair<string, SyntaxTreeNode>>(source.Attributes);
            _start = source.Start;
            SelfClosing = source.SelfClosing;
            SourceStartTag = source.SourceStartTag;
            SourceEndTag = source.SourceEndTag;

            source.Reset();

            foreach (var attributeChildren in Attributes)
            {
                attributeChildren.Value.Parent = this;
            }
        }

        /// <summary>
        /// Gets the unrewritten source start tag.
        /// </summary>
        /// <remarks>This is used by design time to properly format <see cref="TagHelperBlock"/>s.</remarks>
        public Block SourceStartTag { get; }

        /// <summary>
        /// Gets the unrewritten source end tag.
        /// </summary>
        /// <remarks>This is used by design time to properly format <see cref="TagHelperBlock"/>s.</remarks>
        public Block SourceEndTag { get; }

        /// <summary>
        /// Indicates whether or not the tag is self closing.
        /// </summary>
        public bool SelfClosing { get; }

        /// <summary>
        /// <see cref="TagHelperDescriptor"/>s for the HTML element.
        /// </summary>
        public IEnumerable<TagHelperDescriptor> Descriptors { get; }

        /// <summary>
        /// The HTML attributes.
        /// </summary>
        public IList<KeyValuePair<string, SyntaxTreeNode>> Attributes { get; }

        /// <inheritdoc />
        public override SourceLocation Start
        {
            get
            {
                return _start;
            }
        }

        /// <summary>
        /// The HTML tag name.
        /// </summary>
        public string TagName { get; }

        public override int Length
        {
            get
            {
                var startTagLength = SourceStartTag?.Length ?? 0;
                var childrenLength = base.Length;
                var endTagLength = SourceEndTag?.Length ?? 0;

                return startTagLength + childrenLength + endTagLength;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 "'{0}' (Attrs: {1}) Tag Helper Block at {2}::{3} (Gen:{4})",
                                 TagName, Attributes.Count, Start, Length, CodeGenerator);
        }

        /// <summary>
        /// Determines whether two <see cref="TagHelperBlock"/>s are equal by comparing the <see cref="TagName"/>,
        /// <see cref="Attributes"/>, <see cref="Block.Type"/>, <see cref="Block.CodeGenerator"/> and
        /// <see cref="Block.Children"/>.
        /// </summary>
        /// <param name="other">The <see cref="TagHelperBlock"/> to check equality against.</param>
        /// <returns>
        /// <c>true</c> if the current <see cref="TagHelperBlock"/> is equivalent to the given
        /// <paramref name="other"/>, <c>false</c> otherwise.
        /// </returns>
        public bool Equals(TagHelperBlock other)
        {
            return base.Equals(other) &&
                string.Equals(TagName, other.TagName, StringComparison.OrdinalIgnoreCase) &&
                Attributes.SequenceEqual(other.Attributes);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(base.GetHashCode())
                .Add(TagName, StringComparer.OrdinalIgnoreCase)
                .Add(Attributes)
                .CombinedHash;
        }
    }
}