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

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpHelperVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string HelperWriterName = "__razor_helper_writer";

        private CSharpCodeVisitor _codeVisitor;

        public CSharpHelperVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
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
