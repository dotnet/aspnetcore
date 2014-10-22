// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpUsingVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string TagHelpersRuntimeNamespace = "Microsoft.AspNet.Razor.Runtime.TagHelpers";

        private bool _foundTagHelpers;

        public CSharpUsingVisitor(CSharpCodeWriter writer, CodeBuilderContext context)
            : base(writer, context)
        {
            ImportedUsings = new List<string>();
        }

        public IList<string> ImportedUsings { get; set; }

        protected override void Visit(UsingChunk chunk)
        {
            var documentContent = ((Span)chunk.Association).Content.Trim();
            var mapSemicolon = false;

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

        protected override void Visit(TagHelperChunk chunk)
        {
            if (!_foundTagHelpers)
            {
                _foundTagHelpers = true;

                if (!ImportedUsings.Contains(TagHelpersRuntimeNamespace))
                {
                    // If we find TagHelpers then we need to add the TagHelper runtime namespace to our list of usings.
                    Writer.WriteUsing(TagHelpersRuntimeNamespace);
                    ImportedUsings.Add(TagHelpersRuntimeNamespace);
                }
            }
        }
    }
}
