// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Razor.Text;

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
        /// true if the provided node has all the same content and metadata, though the specific quantity and type of symbols may be different.
        /// </returns>
        public abstract bool EquivalentTo(SyntaxTreeNode node);
    }
}
