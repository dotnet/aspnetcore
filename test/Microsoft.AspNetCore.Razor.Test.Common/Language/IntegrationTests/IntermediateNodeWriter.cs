// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    // Serializes single IR nodes (shallow).
    public class IntermediateNodeWriter : IntermediateNodeVisitor, IExtensionIntermediateNodeVisitor<SectionIntermediateNode>
    {
        private readonly TextWriter _writer;

        public IntermediateNodeWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public int Depth { get; set; }

        public override void VisitDefault(IntermediateNode node)
        {
            WriteBasicNode(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            WriteContentNode(node, string.Join(" ", node.Modifiers), node.Name, node.BaseType, string.Join(", ", node.Interfaces ?? new List<string>()));
        }

        public override void VisitCSharpExpressionAttributeValue(CSharpExpressionAttributeValueIntermediateNode node)
        {
            WriteContentNode(node, node.Prefix);
        }

        public override void VisitCSharpCodeAttributeValue(CSharpCodeAttributeValueIntermediateNode node)
        {
            WriteContentNode(node, node.Prefix);
        }

        public override void VisitToken(IntermediateToken node)
        {
            WriteContentNode(node, node.Kind.ToString(), node.Content);
        }

        public override void VisitMalformedDirective(MalformedDirectiveIntermediateNode node)
        {
            WriteContentNode(node, node.Name);
        }

        public override void VisitDirective(DirectiveIntermediateNode node)
        {
            WriteContentNode(node, node.Name);
        }

        public override void VisitDirectiveToken(DirectiveTokenIntermediateNode node)
        {
            WriteContentNode(node, node.Content);
        }

        public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
        {
            WriteContentNode(node, node.Prefix, node.Suffix);
        }

        public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
        {
            WriteContentNode(node, node.Prefix);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
        {
            WriteContentNode(node, node.Content);
        }

        public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
        {
            WriteContentNode(node, string.Join(" ", node.Modifiers), node.ReturnType, node.Name);
        }

        public override void VisitUsingDirective(UsingDirectiveIntermediateNode node)
        {
            WriteContentNode(node, node.Content);
        }

        public override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIntermediateNode node)
        {
            WriteContentNode(node, node.UsedTagHelperTypeNames.ToArray());
        }

        public override void VisitTagHelper(TagHelperIntermediateNode node)
        {
            WriteContentNode(node, node.TagName, string.Format("{0}.{1}", nameof(TagMode), node.TagMode));
        }

        public override void VisitCreateTagHelper(CreateTagHelperIntermediateNode node)
        {
            WriteContentNode(node, node.TagHelperTypeName);
        }

        public override void VisitSetTagHelperProperty(SetTagHelperPropertyIntermediateNode node)
        {
            WriteContentNode(node, node.AttributeName, node.PropertyName, string.Format("HtmlAttributeValueStyle.{0}", node.AttributeStructure));
        }

        public override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIntermediateNode node)
        {
            WriteContentNode(node, node.Name, string.Format("{0}.{1}", nameof(AttributeStructure), node.AttributeStructure));
        }

        public override void VisitExtension(ExtensionIntermediateNode node)
        {
            switch (node)
            {
                case DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode n:
                    WriteContentNode(n, n.VariableName, n.Name, n.Value, string.Format("{0}.{1}", nameof(AttributeStructure), n.AttributeStructure));
                    break;
                case AddPreallocatedTagHelperHtmlAttributeIntermediateNode n:
                    WriteContentNode(n, n.VariableName);
                    break;
                case DeclarePreallocatedTagHelperAttributeIntermediateNode n:
                    WriteContentNode(n, n.VariableName, n.Name, n.Value, string.Format("HtmlAttributeValueStyle.{0}", n.AttributeStructure));
                    break;
                case SetPreallocatedTagHelperPropertyIntermediateNode n:
                    WriteContentNode(n, n.VariableName, n.AttributeName, n.PropertyName);
                    break;
                default:
                    base.VisitExtension(node);
                    break;
            }
        }

        public void VisitExtension(SectionIntermediateNode node)
        {
            WriteContentNode(node, node.Name);
        }

        protected void WriteBasicNode(IntermediateNode node)
        {
            WriteIndent();
            WriteName(node);
            WriteSeparator();
            WriteSourceRange(node);
        }

        protected void WriteContentNode(IntermediateNode node, params string[] content)
        {
            WriteIndent();
            WriteName(node);
            WriteSeparator();
            WriteSourceRange(node);

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

        protected void WriteName(IntermediateNode node)
        {
            var typeName = node.GetType().Name;
            if (typeName.EndsWith("IntermediateNode"))
            {
                _writer.Write(typeName.Substring(0, typeName.Length - "IntermediateNode".Length));
            }
            else
            {
                _writer.Write(typeName);
            }
        }

        protected void WriteSourceRange(IntermediateNode node)
        {
            if (node.Source != null)
            {
                WriteSourceRange(node.Source.Value);
            }
        }

        protected void WriteSourceRange(SourceSpan sourceRange)
        {
            _writer.Write("(");
            _writer.Write(sourceRange.AbsoluteIndex);
            _writer.Write(":");
            _writer.Write(sourceRange.LineIndex);
            _writer.Write(",");
            _writer.Write(sourceRange.CharacterIndex);
            _writer.Write(" [");
            _writer.Write(sourceRange.Length);
            _writer.Write("] ");

            if (sourceRange.FilePath != null)
            {
                var fileName = sourceRange.FilePath.Substring(sourceRange.FilePath.LastIndexOf('/') + 1);
                _writer.Write(fileName);
            }

            _writer.Write(")");
        }

        protected void WriteDiagnostics(IntermediateNode node)
        {
            if (node.HasDiagnostics)
            {
                _writer.Write("| ");
                for (var i = 0; i < node.Diagnostics.Count; i++)
                {
                    var diagnostic = node.Diagnostics[i];
                    _writer.Write("{");
                    WriteSourceRange(diagnostic.Span);
                    _writer.Write(": ");
                    _writer.Write(diagnostic.Severity);
                    _writer.Write(" ");
                    _writer.Write(diagnostic.Id);
                    _writer.Write(": ");

                    // Purposefully not writing out the entire message to ensure readable IR and because messages 
                    // can span multiple lines. Not using string.GetHashCode because we can't have any collisions.
                    using (var md5 = MD5.Create())
                    {
                        var diagnosticMessage = diagnostic.GetMessage();
                        var messageBytes = Encoding.UTF8.GetBytes(diagnosticMessage);
                        var messageHash = md5.ComputeHash(messageBytes);
                        var stringHashBuilder = new StringBuilder();

                        for (var j = 0; j < messageHash.Length; j++)
                        {
                            stringHashBuilder.Append(messageHash[j].ToString("x2"));
                        }

                        var stringHash = stringHashBuilder.ToString();
                        _writer.Write(stringHash);
                    }
                    _writer.Write("} ");
                }
            }
        }

        protected void WriteContent(string content)
        {
            if (content == null)
            {
                return;
            }

            // We explicitly escape newlines in node content so that the IR can be compared line-by-line. The escaped
            // newline cannot be platform specific so we need to drop the windows \r.
            // Also, escape our separator so we can search for ` - `to find delimiters.
            _writer.Write(content.Replace("\r", string.Empty).Replace("\n", "\\n").Replace(" - ", "\\-"));
        }
    }
}
