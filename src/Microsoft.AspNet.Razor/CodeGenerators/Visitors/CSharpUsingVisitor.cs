// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    public class CSharpUsingVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string TagHelpersRuntimeNamespace = "Microsoft.AspNet.Razor.Runtime.TagHelpers";

        private bool _foundTagHelpers;

        public CSharpUsingVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
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

            ImportedUsings = new List<string>();
        }

        public IList<string> ImportedUsings { get; set; }

        /// <inheritdoc />
        public override void Accept(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            // If at any ParentChunk other than a TagHelperChunk, then dive into its Children to search for more
            // TagHelperChunk or UsingChunk nodes. This method avoids overriding each of the ParentChunk-specific
            // Visit() methods to dive into Children.
            var parentChunk = chunk as ParentChunk;
            if (parentChunk != null && !(parentChunk is TagHelperChunk))
            {
                Accept(parentChunk.Children);
            }
            else
            {
                // If at a TagHelperChunk or any non-ParentChunk (e.g. UsingChunk), "Accept()" it. This ensures the
                // Visit(UsingChunk) and Visit(TagHelperChunk) methods below are called.
                base.Accept(chunk);
            }
        }

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
