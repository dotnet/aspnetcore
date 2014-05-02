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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Razor.Generator;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class BlockBuilder
    {
        public BlockBuilder()
        {
            Reset();
        }

        public BlockBuilder(Block original)
        {
            Type = original.Type;
            Children = new List<SyntaxTreeNode>(original.Children);
            Name = original.Name;
            CodeGenerator = original.CodeGenerator;
        }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Type is the most appropriate name for this property and there is little chance of confusion with GetType")]
        public BlockType? Type { get; set; }

        public IList<SyntaxTreeNode> Children { get; private set; }
        public string Name { get; set; }
        public IBlockCodeGenerator CodeGenerator { get; set; }

        public Block Build()
        {
            return new Block(this);
        }

        public void Reset()
        {
            Type = null;
            Name = null;
            Children = new List<SyntaxTreeNode>();
            CodeGenerator = BlockCodeGenerator.Null;
        }
    }
}
