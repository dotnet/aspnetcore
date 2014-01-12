// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class TemplateBlockCodeGenerator : BlockCodeGenerator
    {
        private const string TemplateWriterName = "__razor_template_writer";
        private const string ItemParameterName = "item";

        private string _oldTargetWriter;

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            string generatedCode = context.BuildCodeString(cw =>
            {
                cw.WriteStartLambdaExpression(ItemParameterName);
                cw.WriteStartConstructor(context.Host.GeneratedClassContext.TemplateTypeName);
                cw.WriteStartLambdaDelegate(TemplateWriterName);
            });

            context.MarkEndOfGeneratedCode();
            context.BufferStatementFragment(generatedCode);
            context.FlushBufferedStatement();

            _oldTargetWriter = context.TargetWriterName;
            context.TargetWriterName = TemplateWriterName;
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            string generatedCode = context.BuildCodeString(cw =>
            {
                cw.WriteEndLambdaDelegate();
                cw.WriteEndConstructor();
                cw.WriteEndLambdaExpression();
            });

            context.BufferStatementFragment(generatedCode);
            context.TargetWriterName = _oldTargetWriter;
        }
    }
}
