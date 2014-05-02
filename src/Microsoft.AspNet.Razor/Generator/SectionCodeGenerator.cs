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

using System;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class SectionCodeGenerator : BlockCodeGenerator
    {
        public SectionCodeGenerator(string sectionName)
        {
            SectionName = sectionName;
        }

        public string SectionName { get; private set; }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            var chunk = context.CodeTreeBuilder.StartChunkBlock<SectionChunk>(target);

            chunk.Name = SectionName;
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.EndChunkBlock();
        }

        public override bool Equals(object obj)
        {
            SectionCodeGenerator other = obj as SectionCodeGenerator;
            return other != null &&
                   base.Equals(other) &&
                   String.Equals(SectionName, other.SectionName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(base.GetHashCode())
                .Add(SectionName)
                .CombinedHash;
        }

        public override string ToString()
        {
            return "Section:" + SectionName;
        }
    }
}
