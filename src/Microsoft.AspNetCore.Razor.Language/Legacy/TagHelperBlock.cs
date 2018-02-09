// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    /// <summary>
    /// A <see cref="Block"/> that reprents a special HTML element.
    /// </summary>
    internal class TagHelperBlock : Block, IEquatable<TagHelperBlock>
    {
        private readonly SourceLocation _start;

        /// <summary>
        /// Instantiates a new instance of a <see cref="TagHelperBlock"/>.
        /// </summary>
        /// <param name="source">A <see cref="TagHelperBlockBuilder"/> used to construct a valid
        /// <see cref="TagHelperBlock"/>.</param>
        public TagHelperBlock(TagHelperBlockBuilder source)
            : base(source.Type, source.Children, source.ChunkGenerator)
        {
            TagName = source.TagName;
            Binding = source.BindingResult;
            Attributes = new List<TagHelperAttributeNode>(source.Attributes);
            _start = source.Start;
            TagMode = source.TagMode;
            SourceStartTag = source.SourceStartTag;
            SourceEndTag = source.SourceEndTag;

            source.Reset();

            foreach (var attributeChildren in Attributes)
            {
                if (attributeChildren.Value != null)
                {
                    attributeChildren.Value.Parent = this;
                }
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
        /// Gets the HTML syntax of the element in the Razor source.
        /// </summary>
        public TagMode TagMode { get; }

        /// <summary>
        /// <see cref="TagHelperDescriptor"/> bindings for the HTML element.
        /// </summary>
        public TagHelperBinding Binding { get; }

        /// <summary>
        /// The HTML attributes.
        /// </summary>
        public IList<TagHelperAttributeNode> Attributes { get; }

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

        public override IEnumerable<Span> Flatten()
        {
            if (SourceStartTag != null)
            {
                foreach (var childSpan in SourceStartTag.Flatten())
                {
                    yield return childSpan;
                }
            }

            foreach (var childSpan in base.Flatten())
            {
                yield return childSpan;
            }

            if (SourceEndTag != null)
            {
                foreach (var childSpan in SourceEndTag.Flatten())
                {
                    yield return childSpan;
                }
            }
        }

        public override Span LocateOwner(SourceChange change)
        {
            var oldPosition = change.Span.AbsoluteIndex;
            if (oldPosition < Start.AbsoluteIndex)
            {
                // Change occurs prior to the TagHelper.
                return null;
            }

            var bodyEndLocation = SourceStartTag?.Start.AbsoluteIndex + SourceStartTag?.Length + base.Length;
            if (oldPosition > bodyEndLocation)
            {
                // Change occurs after the TagHelpers body. End tags for TagHelpers cannot claim ownership of changes
                // because any change to them impacts whether or not a tag is a TagHelper.
                return null;
            }

            var startTagEndLocation = Start.AbsoluteIndex + SourceStartTag?.Length;
            if (oldPosition < startTagEndLocation)
            {
                // Change occurs in the start tag.

                var attributeElements = Attributes
                    .Select(attribute => attribute.Value)
                    .Where(value => value != null);

                return LocateOwner(change, attributeElements);
            }

            if (oldPosition < bodyEndLocation)
            {
                // Change occurs in the body
                return base.LocateOwner(change);
            }

            // TagHelper does not contain a Span that can claim ownership.
            return null;
        }

        public override SyntaxTreeNode Clone()
        {
            var tagHelperBlockBuilder = new TagHelperBlockBuilder(this);

            tagHelperBlockBuilder.Children.Clear();
            for (var i = 0; i < Children.Count; i++)
            {
                var clonedChild = Children[i].Clone();
                tagHelperBlockBuilder.Children.Add(clonedChild);
            }

            tagHelperBlockBuilder.Attributes.Clear();
            for (var i = 0; i < Attributes.Count; i++)
            {
                var existingAttribute = Attributes[i];
                var clonedValue = existingAttribute.Value != null ? existingAttribute.Value.Clone() : null;
                tagHelperBlockBuilder.Attributes.Add(
                    new TagHelperAttributeNode(existingAttribute.Name, clonedValue, existingAttribute.AttributeStructure));
            }

            if (SourceStartTag != null)
            {
                var clonedStartTag = (Block)SourceStartTag.Clone();
                tagHelperBlockBuilder.SourceStartTag = clonedStartTag;
            }

            if (SourceEndTag != null)
            {
                var clonedEndTag = (Block)SourceEndTag.Clone();
                tagHelperBlockBuilder.SourceEndTag = clonedEndTag;
            }

            return tagHelperBlockBuilder.Build();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 "'{0}' (Attrs: {1}) Tag Helper Block at {2}::{3} (Gen:{4})",
                                 TagName, Attributes.Count, Start, Length, ChunkGenerator);
        }

        /// <summary>
        /// Determines whether two <see cref="TagHelperBlock"/>s are equal by comparing the <see cref="TagName"/>,
        /// <see cref="Attributes"/>, <see cref="Block.Type"/>, <see cref="Block.ChunkGenerator"/> and
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
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(base.GetHashCode());
            hashCodeCombiner.Add(TagName, StringComparer.OrdinalIgnoreCase);
            hashCodeCombiner.Add(Attributes);

            return hashCodeCombiner;
        }
    }
}