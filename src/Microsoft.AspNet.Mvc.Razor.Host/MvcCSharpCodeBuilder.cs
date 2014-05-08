// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcCSharpCodeBuilder : CSharpCodeBuilder
    {
        public MvcCSharpCodeBuilder([NotNull] CodeGeneratorContext context)
            : base(context)
        {
        }

        protected override void BuildConstructor([NotNull] CSharpCodeWriter writer)
        {
            writer.WriteLineHiddenDirective();

            var injectVisitor = new InjectChunkVisitor(writer, Context);
            injectVisitor.Accept(Context.CodeTreeBuilder.CodeTree.Chunks);

            writer.WriteLine();
            writer.WriteLineHiddenDirective();

            var arguments = injectVisitor.InjectChunks
                                         .Select(chunk => new KeyValuePair<string, string>(chunk.TypeName, 
                                                                                           chunk.MemberName));
            using (writer.BuildConstructor("public", Context.ClassName, arguments))
            {
                foreach (var inject in injectVisitor.InjectChunks)
                {
                    writer.WriteStartAssignment("this." + inject.MemberName)
                          .Write(inject.MemberName)
                          .WriteLine(";");
                }
            }
        }
    }
}