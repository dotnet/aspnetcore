// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpUsingVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpUsingVisitor(CSharpCodeWriter writer, CodeBuilderContext context)
            : base(writer, context)
        {
            ImportedUsings = new List<string>();
        }

        public IList<string> ImportedUsings { get; set; }

        protected override void Visit(UsingChunk chunk)
        {
            string documentContent = ((Span)chunk.Association).Content.Trim();
            bool mapSemicolon = false;

            if (documentContent.LastOrDefault() == ';')
            {
                mapSemicolon = true;
            }

            ImportedUsings.Add(chunk.Namespace);

            // Depending on if the user has a semicolon in their @using statement we have to conditionally decide
            // to include the semicolon in the line mapping.
            using (Writer.BuildLineMapping(chunk.Start, documentContent.Length, Context.SourceFile))
            {
                Writer.WriteUsing(chunk.Namespace, endLine: false);

                if (mapSemicolon)
                {
                    Writer.Write(";");
                }
            }

            if (!mapSemicolon)
            {
                Writer.WriteLine(";");
            }
        }
    }
}
