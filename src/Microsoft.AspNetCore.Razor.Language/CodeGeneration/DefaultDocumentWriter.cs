// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DefaultDocumentWriter : DocumentWriter
    {
        private readonly CSharpRenderingContext _context;
        private readonly CodeTarget _target;

        public DefaultDocumentWriter(CodeTarget target, CSharpRenderingContext context)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _target = target;
            _context = context;
        }

        public override void WriteDocument(DocumentIntermediateNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var visitor = new Visitor(_target, _context);
            _context.RenderChildren = visitor.RenderChildren;
            _context.RenderNode = visitor.Visit;

            _context.BasicWriter = _context.Options.DesignTime ? (BasicWriter)new DesignTimeBasicWriter() : new RuntimeBasicWriter();
            _context.TagHelperWriter = _context.Options.DesignTime ? (TagHelperWriter)new DesignTimeTagHelperWriter() : new RuntimeTagHelperWriter();

            visitor.VisitDocument(node);
            _context.RenderChildren = null;
        }

        private class Visitor : IntermediateNodeVisitor
        {
            private readonly CSharpRenderingContext _context;
            private readonly CodeTarget _target;

            public Visitor(CodeTarget target, CSharpRenderingContext context)
            {
                _target = target;
                _context = context;
            }

            private CSharpRenderingContext Context => _context;

            public void RenderChildren(IntermediateNode node)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    Visit(child);
                }
            }

            public override void VisitDocument(DocumentIntermediateNode node)
            {
                if (!Context.Options.SuppressChecksum)
                {
                    // See http://msdn.microsoft.com/en-us/library/system.codedom.codechecksumpragma.checksumalgorithmid.aspx
                    const string Sha1AlgorithmId = "{ff1816ec-aa5e-4d10-87f7-6f4963833460}";

                    var sourceDocument = Context.SourceDocument;

                    var checksum = sourceDocument.GetChecksum();
                    var fileHashBuilder = new StringBuilder(checksum.Length * 2);
                    foreach (var value in checksum)
                    {
                        fileHashBuilder.Append(value.ToString("x2"));
                    }

                    var bytes = fileHashBuilder.ToString();

                    if (!string.IsNullOrEmpty(bytes))
                    {
                        Context.Writer
                        .Write("#pragma checksum \"")
                        .Write(sourceDocument.FilePath)
                        .Write("\" \"")
                        .Write(Sha1AlgorithmId)
                        .Write("\" \"")
                        .Write(bytes)
                        .WriteLine("\"");
                    }
                }

                RenderChildren(node);
            }

            public override void VisitUsingDirective(UsingDirectiveIntermediateNode node)
            {
                Context.BasicWriter.WriteUsingDirective(Context, node);
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                using (Context.Writer.BuildNamespace(node.Content))
                {
                    Context.Writer.WriteLine("#line hidden");
                    RenderChildren(node);
                }
            }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                using (Context.Writer.BuildClassDeclaration(node.Modifiers, node.Name, node.BaseType, node.Interfaces))
                {
                    RenderChildren(node);
                }
            }

            public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
            {
                Context.Writer.WriteLine("#pragma warning disable 1998");

                for (var i = 0; i < node.Modifiers.Count; i++)
                {
                    Context.Writer.Write(node.Modifiers[i]);
                    Context.Writer.Write(" ");
                }

                Context.Writer
                    .Write(node.ReturnType)
                    .Write(" ")
                    .Write(node.Name)
                    .WriteLine("()");

                using (Context.Writer.BuildScope())
                {
                    RenderChildren(node);
                }

                Context.Writer.WriteLine("#pragma warning restore 1998");
            }

            public override void VisitFieldDeclaration(FieldDeclarationIntermediateNode node)
            {
                Context.Writer.WriteField(node.Modifiers, node.Type, node.Name);
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationIntermediateNode node)
            {
                Context.Writer.WriteAutoPropertyDeclaration(node.Modifiers, node.Type, node.Name);
            }

            public override void VisitExtension(ExtensionIntermediateNode node)
            {
                node.WriteNode(_target, Context);
            }

            public override void VisitCSharpExpression(CSharpExpressionIntermediateNode node)
            {
                Context.BasicWriter.WriteCSharpExpression(Context, node);
            }

            public override void VisitCSharpCode(CSharpCodeIntermediateNode node)
            {
                Context.BasicWriter.WriteCSharpCode(Context, node);
            }

            public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
            {
                Context.BasicWriter.WriteHtmlAttribute(Context, node);
            }

            public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
            {
                Context.BasicWriter.WriteHtmlAttributeValue(Context, node);
            }

            public override void VisitCSharpExpressionAttributeValue(CSharpExpressionAttributeValueIntermediateNode node)
            {
                Context.BasicWriter.WriteCSharpExpressionAttributeValue(Context, node);
            }

            public override void VisitCSharpCodeAttributeValue(CSharpCodeAttributeValueIntermediateNode node)
            {
                Context.BasicWriter.WriteCSharpCodeAttributeValue(Context, node);
            }

            public override void VisitHtml(HtmlContentIntermediateNode node)
            {
                Context.BasicWriter.WriteHtmlContent(Context, node);
            }

            public override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIntermediateNode node)
            {
                Context.TagHelperWriter.WriteDeclareTagHelperFields(Context, node);
            }

            public override void VisitTagHelper(TagHelperIntermediateNode node)
            {
                var tagHelperRenderingContext = new TagHelperRenderingContext()
                {
                    TagName = node.TagName,
                    TagMode = node.TagMode
                };

                using (Context.Push(tagHelperRenderingContext))
                {
                    Context.TagHelperWriter.WriteTagHelper(Context, node);
                }
            }

            public override void VisitTagHelperBody(TagHelperBodyIntermediateNode node)
            {
                Context.TagHelperWriter.WriteTagHelperBody(Context, node);
            }

            public override void VisitCreateTagHelper(CreateTagHelperIntermediateNode node)
            {
                Context.TagHelperWriter.WriteCreateTagHelper(Context, node);
            }

            public override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIntermediateNode node)
            {
                Context.TagHelperWriter.WriteAddTagHelperHtmlAttribute(Context, node);
            }

            public override void VisitSetTagHelperProperty(SetTagHelperPropertyIntermediateNode node)
            {
                Context.TagHelperWriter.WriteSetTagHelperProperty(Context, node);
            }

            public override void VisitDefault(IntermediateNode node)
            {
                Context.RenderChildren(node);
            }
        }
    }
}
