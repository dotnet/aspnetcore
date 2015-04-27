// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class SectionCodeGenerator : BlockCodeGenerator
    {
        public SectionCodeGenerator(string sectionName)
        {
            SectionName = sectionName;
        }

        public string SectionName { get; }

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
            var other = obj as SectionCodeGenerator;
            return base.Equals(other) &&
                string.Equals(SectionName, other.SectionName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return SectionName == null ? 0 : StringComparer.Ordinal.GetHashCode(SectionName);
        }

        public override string ToString()
        {
            return "Section:" + SectionName;
        }
    }
}
