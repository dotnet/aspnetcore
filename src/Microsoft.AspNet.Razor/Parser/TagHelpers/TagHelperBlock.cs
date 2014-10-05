// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
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
            Attributes = new Dictionary<string, SyntaxTreeNode>(source.Attributes);
            _start = source.Start;

            source.Reset();

            foreach (var attributeChildren in Attributes.Values)
            {
                attributeChildren.Parent = this;
            }
        }

        /// <summary>
        /// The HTML attributes.
        /// </summary>
        public IDictionary<string, SyntaxTreeNode> Attributes { get; private set; }

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
        public string TagName { get; private set; }

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
            return other != null &&
                   TagName == other.TagName &&
                   Attributes.SequenceEqual(other.Attributes) &&
                   base.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                                   .Add(TagName)
                                   .Add(Attributes)
                                   .Add(base.GetHashCode())
                                   .CombinedHash;
        }
    }
}