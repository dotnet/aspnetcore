// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    // Serializes single IR nodes (shallow).
    public class RazorIRNodeWriter : RazorIRNodeVisitor
    {
        private readonly TextWriter _writer;

        public RazorIRNodeWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public int Depth { get; set; }

        public override void VisitDefault(RazorIRNode node)
        {
            WriteBasicNode(node);
        }

        public override void VisitClass(ClassDeclarationIRNode node)
        {
            WriteContentNode(node, node.AccessModifier, node.Name, node.BaseType, string.Join(", ", node.Interfaces ?? new List<string>()));
        }

        public override void VisitCSharpAttributeValue(CSharpAttributeValueIRNode node)
        {
            WriteContentNode(node, node.Prefix);
        }

        public override void VisitCSharpStatement(CSharpStatementIRNode node)
        {
            WriteContentNode(node, node.Content);
        }

        public override void VisitCSharpToken(CSharpTokenIRNode node)
        {
            WriteContentNode(node, node.Content);
        }

        public override void VisitDirective(DirectiveIRNode node)
        {
            WriteContentNode(node, node.Name);
        }

        public override void VisitDirectiveToken(DirectiveTokenIRNode node)
        {
            WriteContentNode(node, node.Content);
        }

        public override void VisitHtml(HtmlContentIRNode node)
        {
            WriteContentNode(node, node.Content);
        }

        public override void VisitHtmlAttribute(HtmlAttributeIRNode node)
        {
            WriteContentNode(node, node.Prefix, node.Suffix);
        }

        public override void VisitHtmlAttributeValue(HtmlAttributeValueIRNode node)
        {
            WriteContentNode(node, node.Prefix, node.Content);
        }

        public override void VisitNamespace(NamespaceDeclarationIRNode node)
        {
            WriteContentNode(node, node.Content);
        }

        public override void VisitRazorMethodDeclaration(RazorMethodDeclarationIRNode node)
        {
            WriteContentNode(node, node.AccessModifier, string.Join(", ", node.Modifiers ?? new List<string>()), node.ReturnType, node.Name);
        }

        public override void VisitUsingStatement(UsingStatementIRNode node)
        {
            WriteContentNode(node, node.Content);
        }

        protected void WriteBasicNode(RazorIRNode node)
        {
            WriteIndent();
            WriteName(node);
            WriteSeparator();
            WriteLocation(node);
        }

        protected void WriteContentNode(RazorIRNode node, params string[] content)
        {
            WriteIndent();
            WriteName(node);
            WriteSeparator();
            WriteLocation(node);

            for (var i = 0; i < content.Length; i++)
            {
                WriteSeparator();
                WriteContent(content[i]);
            }
        }

        protected void WriteIndent()
        {
            for (var i = 0; i < Depth; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    _writer.Write(' ');
                }
            }
        }

        protected void WriteSeparator()
        {
            _writer.Write(" - ");
        }

        protected void WriteNewLine()
        {
            _writer.WriteLine();
        }

        protected void WriteName(RazorIRNode node)
        {
            var typeName = node.GetType().Name;
            if (typeName.EndsWith("IRNode"))
            {
                _writer.Write(typeName.Substring(0, typeName.Length - "IRNode".Length));
            }
            else
            {
                _writer.Write(typeName);
            }
        }

        protected void WriteLocation(RazorIRNode node)
        {
            _writer.Write(node.SourceLocation.ToString());
        }

        protected void WriteContent(string content)
        {
            if (content == null)
            {
                return;
            }

            // We explicitly escape newlines in node content so that the IR can be compared line-by-line.
            // Also, escape our separator so we can search for ` - `to find delimiters.
            _writer.Write(content.Replace("\r", "\\r").Replace("\n", "\\n").Replace("-", "\\-"));
        }
    }
}
