// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    public class CSharpDesignTimeCodeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string InheritsHelper = "__inheritsHelper";
        private const string DesignTimeHelperMethodName = "__RazorDesignTimeHelpers__";
        private const string TagHelperDirectiveSyntaxHelper = "__tagHelperDirectiveSyntaxHelper";
        private const int DisableVariableNamingWarnings = 219;

        private bool _initializedTagHelperDirectiveSyntaxHelper;

        public CSharpDesignTimeCodeVisitor(
            CSharpCodeVisitor csharpCodeVisitor,
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
            : base(writer, context)
        {
            if (csharpCodeVisitor == null)
            {
                throw new ArgumentNullException(nameof(csharpCodeVisitor));
            }

            CSharpCodeVisitor = csharpCodeVisitor;
        }

        public CSharpCodeVisitor CSharpCodeVisitor { get; }

        public void AcceptTree(ChunkTree tree)
        {
            if (Context.Host.DesignTimeMode)
            {
                using (Writer.BuildMethodDeclaration("private", "void", "@" + DesignTimeHelperMethodName))
                {
                    using (Writer.BuildDisableWarningScope(DisableVariableNamingWarnings))
                    {
                        AcceptTreeCore(tree);
                    }
                }
            }
        }

        protected virtual void AcceptTreeCore(ChunkTree tree)
        {
            Accept(tree.Children);
        }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            Debug.Assert(Context.Host.DesignTimeMode);

            if (chunk.Start != SourceLocation.Undefined)
            {
                using (var lineMappingWriter =
                    Writer.BuildLineMapping(chunk.Start, chunk.TypeName.Length, Context.SourceFile))
                {
                    Writer.Indent(chunk.Start.CharacterIndex);

                    lineMappingWriter.MarkLineMappingStart();
                    Writer.Write(chunk.TypeName);
                    lineMappingWriter.MarkLineMappingEnd();

                    Writer.Write(" ").Write(InheritsHelper).Write(" = null;");
                }
            }
        }

        protected override void Visit(TagHelperPrefixDirectiveChunk chunk)
        {
            VisitTagHelperDirectiveChunk(chunk.Prefix, chunk);
        }

        protected override void Visit(AddTagHelperChunk chunk)
        {
            VisitTagHelperDirectiveChunk(chunk.LookupText, chunk);
        }

        protected override void Visit(RemoveTagHelperChunk chunk)
        {
            VisitTagHelperDirectiveChunk(chunk.LookupText, chunk);
        }

        private void VisitTagHelperDirectiveChunk(string text, Chunk chunk)
        {
            // We should always be in design time mode because of the calling AcceptTree method verification.
            Debug.Assert(Context.Host.DesignTimeMode);

            if (!_initializedTagHelperDirectiveSyntaxHelper)
            {
                _initializedTagHelperDirectiveSyntaxHelper = true;
                Writer.WriteVariableDeclaration("string", TagHelperDirectiveSyntaxHelper, "null");
            }

            Writer
                .WriteStartAssignment(TagHelperDirectiveSyntaxHelper)
                .Write("\"");

            using (new CSharpLineMappingWriter(Writer, chunk.Start, text.Length))
            {
                Writer.Write(text);
            }

            Writer.WriteLine("\";");
        }
    }
}
