// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpHelperVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string HelperWriterName = "__razor_helper_writer";

        private CSharpCodeVisitor _codeVisitor;

        public CSharpHelperVisitor(CSharpCodeWriter writer, CodeBuilderContext context)
            : base(writer, context)
        {
            _codeVisitor = new CSharpCodeVisitor(writer, context);
        }

        protected override void Visit(HelperChunk chunk)
        {
            IDisposable lambdaScope = null;

            string accessibility = "public " + (Context.Host.StaticHelpers ? "static" : String.Empty);

            // We want to write the method signature at 0 indentation so if helper's are formatted they format correctly.
            int currentIndentation = Writer.CurrentIndent;
            Writer.ResetIndent();
            Writer.Write(accessibility).Write(" ").Write(Context.Host.GeneratedClassContext.TemplateTypeName).Write(" ");
            Writer.SetIndent(currentIndentation);

            using (Writer.BuildLineMapping(chunk.Signature.Location, chunk.Signature.Value.Length, Context.SourceFile))
            {
                Writer.Write(chunk.Signature);
            }

            if (chunk.HeaderComplete)
            {
                Writer.WriteStartReturn()
                       .WriteStartNewObject(Context.Host.GeneratedClassContext.TemplateTypeName);

                lambdaScope = Writer.BuildLambda(endLine: false, parameterNames: HelperWriterName);
            }

            string currentTargetWriterName = Context.TargetWriterName;
            Context.TargetWriterName = HelperWriterName;

            // Generate children code
            _codeVisitor.Accept(chunk.Children);

            Context.TargetWriterName = currentTargetWriterName;

            if (chunk.HeaderComplete)
            {
                lambdaScope.Dispose();
                Writer.WriteEndMethodInvocation();
            }

            if (chunk.Footer != null && !String.IsNullOrEmpty(chunk.Footer.Value))
            {
                using (Writer.BuildLineMapping(chunk.Footer.Location, chunk.Footer.Value.Length, Context.SourceFile))
                {
                    Writer.Write(chunk.Footer);
                }
            }

            Writer.WriteLine();
        }
    }
}
