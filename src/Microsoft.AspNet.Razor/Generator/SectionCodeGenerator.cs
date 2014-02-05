// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

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

        public void GenerateStartBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            SectionChunk chunk = codeTreeBuilder.StartChunkBlock<SectionChunk>(target);

            chunk.Name = SectionName;
        }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            string startBlock = context.BuildCodeString(cw =>
            {
                cw.WriteStartMethodInvoke(context.Host.GeneratedClassContext.DefineSectionMethodName);
                cw.WriteStringLiteral(SectionName);
                cw.WriteParameterSeparator();
                cw.WriteStartLambdaDelegate();
            });
            context.AddStatement(startBlock);
#endif

            // TODO: Make this generate the primary generator
            GenerateStartBlockCode(target, context.CodeTreeBuilder, context);
        }

        public void GenerateEndBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.EndChunkBlock();
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            string startBlock = context.BuildCodeString(cw =>
            {
                cw.WriteEndLambdaDelegate();
                cw.WriteEndMethodInvoke();
                cw.WriteEndStatement();
            });
            context.AddStatement(startBlock);
#endif

            // TODO: Make this generate the primary generator
            GenerateEndBlockCode(target, context.CodeTreeBuilder, context);
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
