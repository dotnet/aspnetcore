// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    public class CSharpDesignTimeHelpersVisitor : CodeVisitor<CSharpCodeWriter>
    {
        internal const string InheritsHelper = "__inheritsHelper";
        internal const string DesignTimeHelperMethodName = "__RazorDesignTimeHelpers__";

        private const string TagHelperDirectiveSyntaxHelper = "__tagHelperDirectiveSyntaxHelper";
        private const int DisableVariableNamingWarnings = 219;

        private readonly CSharpCodeVisitor _csharpCodeVisitor;

        private bool _initializedTagHelperDirectiveSyntaxHelper;

        public CSharpDesignTimeHelpersVisitor(CSharpCodeVisitor csharpCodeVisitor,
                                              CSharpCodeWriter writer,
                                              CodeGeneratorContext context)

            : base(writer, context)
        {
            if (csharpCodeVisitor == null)
            {
                throw new ArgumentNullException(nameof(csharpCodeVisitor));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _csharpCodeVisitor = csharpCodeVisitor;
        }

        public void AcceptTree(ChunkTree tree)
        {
            if (Context.Host.DesignTimeMode)
            {
                using (Writer.BuildMethodDeclaration("private", "void", "@" + DesignTimeHelperMethodName))
                {
                    using (Writer.BuildDisableWarningScope(DisableVariableNamingWarnings))
                    {
                        Accept(tree.Chunks);
                    }
                }
            }
        }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            if (Context.Host.DesignTimeMode)
            {
                using (CSharpLineMappingWriter lineMappingWriter = Writer.BuildLineMapping(chunk.Start, chunk.TypeName.Length, Context.SourceFile))
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

            Writer.WriteStartAssignment(TagHelperDirectiveSyntaxHelper);

            // The parsing mechanism for a TagHelper directive chunk (CSharpCodeParser.TagHelperDirective())
            // removes quotes that surround the text.
            _csharpCodeVisitor.CreateExpressionCodeMapping(
                string.Format(CultureInfo.InvariantCulture, "\"{0}\"", text),
                chunk);

            Writer.WriteLine(";");
        }
    }
}
