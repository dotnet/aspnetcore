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

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpUsingVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpUsingVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
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
