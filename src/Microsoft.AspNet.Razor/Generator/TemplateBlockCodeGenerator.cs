// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class TemplateBlockCodeGenerator : BlockCodeGenerator
    {
        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.StartChunkBlock<TemplateChunk>(target);
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            context.CodeTreeBuilder.EndChunkBlock();
        }
    }
}
