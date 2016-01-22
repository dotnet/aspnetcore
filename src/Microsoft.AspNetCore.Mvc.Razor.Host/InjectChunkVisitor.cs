// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class InjectChunkVisitor : MvcCSharpCodeVisitor
    {
        private readonly string _injectAttribute;

        public InjectChunkVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context,
            string injectAttributeName)
            : base(writer, context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (injectAttributeName == null)
            {
                throw new ArgumentNullException(nameof(injectAttributeName));
            }

            _injectAttribute = "[" + injectAttributeName + "]";
        }

        public IList<InjectChunk> InjectChunks { get; } = new List<InjectChunk>();

        protected override void Visit(InjectChunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            Writer.WriteLine(_injectAttribute);

            // Some of the chunks that we visit are either InjectDescriptors that are added by default or
            // are chunks from _ViewStart files and are not associated with any Spans. Invoking
            // CreateExpressionMapping to produce line mappings on these chunks would fail. We'll skip
            // generating code mappings for these chunks. This makes sense since the chunks do not map
            // to any code in the current view.
            if (Context.Host.DesignTimeMode && chunk.Association != null)
            {
                Writer.WriteLine("public");

                var code = string.IsNullOrEmpty(chunk.MemberName) ?
                            chunk.TypeName :
                            chunk.TypeName + " " + chunk.MemberName;
                var csharpVisitor = new CSharpCodeVisitor(Writer, Context);
                csharpVisitor.CreateExpressionCodeMapping(code, chunk);
                Writer.WriteLine("{ get; private set; }");
            }
            else
            {
                Writer.Write("public ")
                      .Write(chunk.TypeName)
                      .Write(" ")
                      .Write(chunk.MemberName)
                      .WriteLine(" { get; private set; }");
            }

            InjectChunks.Add(chunk);
        }
    }
}