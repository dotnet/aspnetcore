// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal class SyntaxSerializer
{
    internal static string Serialize(SyntaxNode node)
    {
        using (var writer = new StringWriter())
        {
            var walker = new Walker(writer);
            walker.Visit(node);

            return writer.ToString();
        }
    }

    private class Walker : SyntaxWalker
    {
        private readonly SyntaxWriter _visitor;
        private readonly TextWriter _writer;

        public Walker(TextWriter writer)
        {
            _visitor = new SyntaxWriter(writer);
            _writer = writer;
        }

        public TextWriter Writer { get; }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return node;
            }

            if (node.IsList)
            {
                return base.DefaultVisit(node);
            }

            _visitor.Visit(node);
            _writer.WriteLine();

            if (!node.IsToken && !node.IsTrivia)
            {
                _visitor.Depth++;
                node = base.DefaultVisit(node);
                _visitor.Depth--;
            }

            return node;
        }
    }

    private class SyntaxWalker : SyntaxRewriter
    {
        private readonly List<SyntaxNode> _ancestors = new List<SyntaxNode>();

        protected IReadOnlyList<SyntaxNode> Ancestors => _ancestors;

        protected SyntaxNode Parent => _ancestors.Count > 0 ? _ancestors[0] : null;

        protected override SyntaxNode DefaultVisit(SyntaxNode node)
        {
            _ancestors.Insert(0, node);

            try
            {
                for (var i = 0; i < node.SlotCount; i++)
                {
                    var child = node.GetNodeSlot(i);
                    Visit(child);
                }
            }
            finally
            {
                _ancestors.RemoveAt(0);
            }

            return node;
        }
    }

    private class SyntaxWriter : SyntaxRewriter
    {
        private readonly TextWriter _writer;
        private bool _visitedRoot;

        public SyntaxWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public int Depth { get; set; }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node is SyntaxToken token)
            {
                return VisitToken(token);
            }

            WriteNode(node);
            return node;
        }

        public override SyntaxNode VisitToken(SyntaxToken token)
        {
            WriteToken(token);
            return base.VisitToken(token);
        }

        public override SyntaxNode VisitTrivia(SyntaxTrivia trivia)
        {
            WriteTrivia(trivia);
            return base.VisitTrivia(trivia);
        }

        private void WriteNode(SyntaxNode node)
        {
            WriteIndent();
            Write(node.Kind);
            WriteSeparator();
            Write($"[{node.Position}..{node.EndPosition})");
            WriteSeparator();
            Write($"FullWidth: {node.FullWidth}");

            if (node is RazorDirectiveSyntax razorDirective)
            {
                WriteRazorDirective(razorDirective);
            }
            else if (node is MarkupTagHelperElementSyntax tagHelperElement)
            {
                WriteTagHelperElement(tagHelperElement);
            }
            else if (node is MarkupTagHelperAttributeSyntax tagHelperAttribute)
            {
                WriteTagHelperAttributeInfo(tagHelperAttribute.TagHelperAttributeInfo);
            }
            else if (node is MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute)
            {
                WriteTagHelperAttributeInfo(minimizedTagHelperAttribute.TagHelperAttributeInfo);
            }
            else if (node is MarkupStartTagSyntax startTag)
            {
                if (startTag.IsMarkupTransition)
                {
                    WriteSeparator();
                    Write("MarkupTransition");
                }
            }
            else if (node is MarkupEndTagSyntax endTag)
            {
                if (endTag.IsMarkupTransition)
                {
                    WriteSeparator();
                    Write("MarkupTransition");
                }
            }

            if (ShouldDisplayNodeContent(node))
            {
                WriteSeparator();
                Write($"[{node.GetContent()}]");
            }

            var annotation = node.GetAnnotations().FirstOrDefault(a => a.Kind == SyntaxConstants.SpanContextKind);
            if (annotation != null && annotation.Data is SpanContext context)
            {
                WriteSpanContext(context);
            }

            if (!_visitedRoot)
            {
                WriteSeparator();
                Write($"[{node.ToFullString()}]");
                _visitedRoot = true;
            }
        }

        private void WriteRazorDirective(RazorDirectiveSyntax node)
        {
            if (node.DirectiveDescriptor == null)
            {
                return;
            }

            var builder = new StringBuilder("Directive:{");
            builder.Append(node.DirectiveDescriptor.Directive);
            builder.Append(';');
            builder.Append(node.DirectiveDescriptor.Kind);
            builder.Append(';');
            builder.Append(node.DirectiveDescriptor.Usage);
            builder.Append('}');

            var diagnostics = node.GetDiagnostics();
            if (diagnostics.Length > 0)
            {
                builder.Append(" [");
                var ids = string.Join(", ", diagnostics.Select(diagnostic => $"{diagnostic.Id}{diagnostic.Span}"));
                builder.Append(ids);
                builder.Append(']');
            }

            WriteSeparator();
            Write(builder.ToString());
        }

        private void WriteTagHelperElement(MarkupTagHelperElementSyntax node)
        {
            // Write tag name
            WriteSeparator();
            Write($"{node.TagHelperInfo.TagName}[{node.TagHelperInfo.TagMode}]");

            // Write descriptors
            foreach (var descriptor in node.TagHelperInfo.BindingResult.Descriptors)
            {
                WriteSeparator();

                // Get the type name without the namespace.
                var typeName = descriptor.Name.Substring(descriptor.Name.LastIndexOf('.') + 1);
                Write(typeName);
            }
        }

        private void WriteTagHelperAttributeInfo(TagHelperAttributeInfo info)
        {
            // Write attributes
            WriteSeparator();
            Write(info.Name);
            WriteSeparator();
            Write(info.AttributeStructure);
            WriteSeparator();
            Write(info.Bound ? "Bound" : "Unbound");
        }

        private void WriteToken(SyntaxToken token)
        {
            WriteIndent();
            var content = token.IsMissing ? "<Missing>" : token.Content;
            var diagnostics = token.GetDiagnostics();
            var tokenString = $"{token.Kind};[{content}];{string.Join(", ", diagnostics.Select(diagnostic => diagnostic.Id + diagnostic.Span))}";
            Write(tokenString);
        }

        private void WriteTrivia(SyntaxTrivia trivia)
        {
            throw new NotImplementedException();
        }

        private void WriteSpanContext(SpanContext context)
        {
            WriteSeparator();
            Write($"Gen<{context.ChunkGenerator}>");
            WriteSeparator();
            Write(context.EditHandler);
        }

        protected void WriteIndent()
        {
            for (var i = 0; i < Depth; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    Write(' ');
                }
            }
        }

        protected void WriteSeparator()
        {
            Write(" - ");
        }

        protected void WriteNewLine()
        {
            _writer.WriteLine();
        }

        protected void Write(object value)
        {
            if (value is string stringValue)
            {
                stringValue = stringValue.Replace(Environment.NewLine, "LF");
                _writer.Write(stringValue);
                return;
            }

            _writer.Write(value);
        }

        private static bool ShouldDisplayNodeContent(SyntaxNode node)
        {
            return node.Kind == SyntaxKind.MarkupTextLiteral ||
                node.Kind == SyntaxKind.MarkupEphemeralTextLiteral ||
                node.Kind == SyntaxKind.MarkupStartTag ||
                node.Kind == SyntaxKind.MarkupEndTag ||
                node.Kind == SyntaxKind.MarkupTagHelperStartTag ||
                node.Kind == SyntaxKind.MarkupTagHelperEndTag ||
                node.Kind == SyntaxKind.MarkupAttributeBlock ||
                node.Kind == SyntaxKind.MarkupMinimizedAttributeBlock ||
                node.Kind == SyntaxKind.MarkupTagHelperAttribute ||
                node.Kind == SyntaxKind.MarkupMinimizedTagHelperAttribute ||
                node.Kind == SyntaxKind.MarkupLiteralAttributeValue ||
                node.Kind == SyntaxKind.MarkupDynamicAttributeValue ||
                node.Kind == SyntaxKind.CSharpStatementLiteral ||
                node.Kind == SyntaxKind.CSharpExpressionLiteral ||
                node.Kind == SyntaxKind.CSharpEphemeralTextLiteral ||
                node.Kind == SyntaxKind.UnclassifiedTextLiteral;
        }
    }
}
