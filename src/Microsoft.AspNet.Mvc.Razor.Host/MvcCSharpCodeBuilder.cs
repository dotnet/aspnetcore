// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcCSharpCodeBuilder : CSharpCodeBuilder
    {
        public MvcCSharpCodeBuilder([NotNull] CodeGeneratorContext context)
            : base(context)
        {
        }

        protected override CSharpCodeWritingScope BuildClassDeclaration(CSharpCodeWriter writer)
        {
            var modelChunks = Context.CodeTreeBuilder.CodeTree.Chunks.OfType<ModelChunk>();

            // If there were any model chunks then we need to modify the class declaration signature.
            if (modelChunks.Any())
            {
                writer.Write(string.Format(CultureInfo.CurrentCulture, "public class {0} : ", Context.ClassName));

                // Grab the last model chunk so it gets intellisense.
                // NOTE: If there's more than 1 model chunk there will be a Razor error BUT we want intellisense to show up
                // on the current model chunk that the user is typing.
                var lastModelChunk = modelChunks.Last();
                var modelVisitor = new ModelChunkVisitor(writer, Context);
                // This generates the base class signature
                modelVisitor.Accept(lastModelChunk);

                writer.WriteLine();

                return new CSharpCodeWritingScope(writer);
            }
            else
            {
                return base.BuildClassDeclaration(writer);
            }
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