// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class InjectChunkVisitor : MvcCSharpCodeVisitor
    {
        private readonly List<InjectChunk> _injectChunks = new List<InjectChunk>();
        private readonly string _activateAttribute;

        public InjectChunkVisitor([NotNull] CSharpCodeWriter writer,
                                  [NotNull] CodeBuilderContext context,
                                  [NotNull] string activateAttributeName)
            : base(writer, context)
        {
            _activateAttribute = '[' + activateAttributeName + ']';
        }

        public List<InjectChunk> InjectChunks
        {
            get { return _injectChunks; }
        }

        protected override void Visit([NotNull] InjectChunk chunk)
        {
            Writer.WriteLine(_activateAttribute);

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
                            chunk.TypeName + ' ' + chunk.MemberName;
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
            _injectChunks.Add(chunk);
        }
    }
}