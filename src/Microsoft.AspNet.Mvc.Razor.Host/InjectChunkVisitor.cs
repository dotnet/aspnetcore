// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.CodeGenerators.Visitors;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class InjectChunkVisitor : MvcCSharpCodeVisitor
    {
        private readonly string _injectAttribute;

        public InjectChunkVisitor([NotNull] CSharpCodeWriter writer,
                                  [NotNull] CodeGeneratorContext context,
                                  [NotNull] string injectAttributeName)
            : base(writer, context)
        {
            _injectAttribute = "[" + injectAttributeName + "]";
        }

        public IList<InjectChunk> InjectChunks { get; } = new List<InjectChunk>();

        protected override void Visit([NotNull] InjectChunk chunk)
        {
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