// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public abstract class SyntaxTreeNode
    {
        public Block Parent { get; internal set; }

        /// <summary>
        /// Returns true if this element is a block (to avoid casting)
        /// </summary>
        public abstract bool IsBlock { get; }

        /// <summary>
        /// The length of all the content contained in this node
        /// </summary>
        public abstract int Length { get; }

        /// <summary>
        /// The start point of this node
        /// </summary>
        public abstract SourceLocation Start { get; }

        /// <summary>
        /// Accepts a parser visitor, calling the appropriate visit method and passing in this instance
        /// </summary>
        /// <param name="visitor">The visitor to accept</param>
        public abstract void Accept(ParserVisitor visitor);

        /// <summary>
        /// Determines if the specified node is equivalent to this node
        /// </summary>
        /// <param name="node">The node to compare this node with</param>
        /// <returns>
        /// true if the provided node has all the same content and metadata, though the specific quantity and type of
        /// symbols may be different.
        /// </returns>
        public abstract bool EquivalentTo(SyntaxTreeNode node);

        /// <summary>
        /// Determines a hash code for the <see cref="SyntaxTreeNode"/> using only information relevant in
        /// <see cref="EquivalentTo"/> comparisons.
        /// </summary>
        /// <returns>
        /// A hash code for the <see cref="SyntaxTreeNode"/> using only information relevant in
        /// <see cref="EquivalentTo"/> comparisons.
        /// </returns>
        public abstract int GetEquivalenceHash();
    }
}
