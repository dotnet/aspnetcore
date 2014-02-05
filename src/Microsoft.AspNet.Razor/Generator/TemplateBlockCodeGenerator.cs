// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class TemplateBlockCodeGenerator : BlockCodeGenerator
    {
        internal const string TemplateWriterName = "__razor_template_writer";
        internal const string ItemParameterName = "item";

        private string _oldTargetWriter;

        public void GenerateStartBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.StartChunkBlock<TemplateChunk>(target);
        }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            string generatedCode = context.BuildCodeString(cw =>
            {
                cw.WriteStartLambdaExpression(ItemParameterName);
                cw.WriteStartConstructor(context.Host.GeneratedClassContext.TemplateTypeName);
                cw.WriteStartLambdaDelegate(TemplateWriterName);
            });

            context.MarkEndOfGeneratedCode();
            context.BufferStatementFragment(generatedCode);
            context.FlushBufferedStatement();
#endif

            _oldTargetWriter = context.TargetWriterName;
            context.TargetWriterName = TemplateWriterName;

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

            string generatedCode = context.BuildCodeString(cw =>
            {
                cw.WriteEndLambdaDelegate();
                cw.WriteEndConstructor();
                cw.WriteEndLambdaExpression();
            });

            context.BufferStatementFragment(generatedCode);
#endif
            context.TargetWriterName = _oldTargetWriter;

            // TODO: Make this generate the primary generator
            GenerateEndBlockCode(target, context.CodeTreeBuilder, context);
        }
    }
}
