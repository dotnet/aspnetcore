// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class StatementCodeGenerator : SpanCodeGenerator
    {
        public void GenerateCode(Span target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.AddStatementChunk(target.Content, target);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            context.FlushBufferedStatement();

            string generatedCode = context.BuildCodeString(cw =>
            {
                cw.WriteSnippet(target.Content);
            });

            int startGeneratedCode = target.Start.CharacterIndex;
            int paddingCharCount;
            generatedCode = CodeGeneratorPaddingHelper.PadStatement(context.Host, generatedCode, target, ref startGeneratedCode, out paddingCharCount);

            context.AddStatement(
                generatedCode,
                context.GenerateLinePragma(target, paddingCharCount));
#endif

            // TODO: Make this generate the primary generator
            GenerateCode(target, context.CodeTreeBuilder, context);
        }

        public override string ToString()
        {
            return "Stmt";
        }

        public override bool Equals(object obj)
        {
            return obj is StatementCodeGenerator;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
