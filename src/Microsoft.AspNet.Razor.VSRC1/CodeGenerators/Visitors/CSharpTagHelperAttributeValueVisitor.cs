// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    /// <summary>
    /// <see cref="CodeVisitor{CSharpCodeWriter}"/> that writes code for a non-<see langword="string"/> tag helper
    /// bound attribute value.
    /// </summary>
    /// <remarks>
    /// Since attribute value is not written out as HTML, does not emit instrumentation. Further this
    /// <see cref="CodeVisitor{CSharpCodeWriter}"/> writes identical code at design- and runtime.
    /// </remarks>
    public class CSharpTagHelperAttributeValueVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private string _attributeTypeName;
        private bool _firstChild;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpTagHelperAttributeValueVisitor"/> class.
        /// </summary>
        /// <param name="writer">The <see cref="CSharpCodeWriter"/> used to write code.</param>
        /// <param name="context">
        /// A <see cref="CodeGeneratorContext"/> instance that contains information about the current code generation
        /// process.
        /// </param>
        /// <param name="attributeTypeName">
        /// Full name of the property <see cref="System.Type"/> for which this
        /// <see cref="CSharpTagHelperAttributeValueVisitor"/> is writing the value.
        /// </param>
        public CSharpTagHelperAttributeValueVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context,
            string attributeTypeName)
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

            _attributeTypeName = attributeTypeName;
        }

        /// <summary>
        /// Writes code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The <see cref="ParentChunk"/> to render.</param>
        /// <remarks>
        /// Tracks code mappings for all children while writing.
        /// </remarks>
        protected override void Visit(ParentChunk chunk)
        {
            // Line mappings are captured in RenderCode(), not this method.
            _firstChild = true;
            Accept(chunk.Children);

            if (_firstChild)
            {
                // Attribute value was empty.
                Context.ErrorSink.OnError(
                    chunk.Association.Start,
                    RazorResources.TagHelpers_AttributeExpressionRequired,
                    chunk.Association.Length);
            }
        }

        /// <summary>
        /// Writes code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The <see cref="ExpressionBlockChunk"/> to render.</param>
        protected override void Visit(ExpressionBlockChunk chunk)
        {
            Accept(chunk.Children);
        }

        /// <summary>
        /// Writes code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The <see cref="ExpressionChunk"/> to render.</param>
        protected override void Visit(ExpressionChunk chunk)
        {
            RenderCode(chunk.Code, (Span)chunk.Association);
        }

        /// <summary>
        /// Writes code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The <see cref="LiteralChunk"/> to render.</param>
        protected override void Visit(LiteralChunk chunk)
        {
            RenderCode(chunk.Text, (Span)chunk.Association);
        }

        /// <summary>
        /// Writes code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The <see cref="SectionChunk"/> to render.</param>
        /// <remarks>
        /// Unconditionally adds a <see cref="RazorError"/> to inform user of unexpected <c>@section</c> directive.
        /// </remarks>
        protected override void Visit(SectionChunk chunk)
        {
            Context.ErrorSink.OnError(
                chunk.Association.Start,
                RazorResources.FormatTagHelpers_Directives_NotSupported_InAttributes(
                    SyntaxConstants.CSharp.SectionKeyword),
                chunk.Association.Length);
        }

        /// <summary>
        /// Writes code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The <see cref="StatementChunk"/> to render.</param>
        /// <remarks>
        /// Unconditionally adds a <see cref="RazorError"/> to inform user of unexpected code block.
        /// </remarks>
        protected override void Visit(StatementChunk chunk)
        {
            Context.ErrorSink.OnError(
                chunk.Association.Start,
                RazorResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                chunk.Association.Length);
        }

        /// <summary>
        /// Writes code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The <see cref="TemplateChunk"/> to render.</param>
        /// <remarks>
        /// Unconditionally adds a <see cref="RazorError"/> to inform user of unexpected template e.g.
        /// <c>@&lt;p&gt;paragraph@&lt;/p&gt;</c>.
        /// </remarks>
        protected override void Visit(TemplateChunk chunk)
        {
            Context.ErrorSink.OnError(
                chunk.Association.Start,
                RazorResources.FormatTagHelpers_InlineMarkupBlocks_NotSupported_InAttributes(_attributeTypeName),
                chunk.Association.Length);
        }

        // Tracks the code mapping and writes code for a leaf node in the attribute value Chunk tree.
        private void RenderCode(string code, Span association)
        {
            _firstChild = false;
            using (new CSharpLineMappingWriter(Writer, association.Start, code.Length))
            {
                Writer.Write(code);
            }
        }
    }
}
