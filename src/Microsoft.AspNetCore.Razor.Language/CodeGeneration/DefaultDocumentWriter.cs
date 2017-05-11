// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DefaultDocumentWriter : DocumentWriter
    {
        private readonly CSharpRenderingContext _context;
        private readonly RuntimeTarget _target;

        public DefaultDocumentWriter(RuntimeTarget target, CSharpRenderingContext context)
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

        public override void WriteDocument(DocumentIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var visitor = new Visitor(_target, _context);
            _context.RenderChildren = visitor.RenderChildren;
            _context.RenderNode = visitor.Visit;

            _context.BasicWriter = _context.Options.DesignTimeMode ? (BasicWriter)new DesignTimeBasicWriter() : new RuntimeBasicWriter();
            _context.TagHelperWriter = _context.Options.DesignTimeMode ? (TagHelperWriter)new DesignTimeTagHelperWriter() : new RuntimeTagHelperWriter();

            visitor.VisitDocument(node);
            _context.RenderChildren = null;
        }

        private class Visitor : RazorIRNodeVisitor
        {
            private readonly CSharpRenderingContext _context;
            private readonly RuntimeTarget _target;

            public Visitor(RuntimeTarget target, CSharpRenderingContext context)
            {
                _target = target;
                _context = context;
            }

            private CSharpRenderingContext Context => _context;

            public void RenderChildren(RazorIRNode node)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    Visit(child);
                }
            }

            public override void VisitDocument(DocumentIRNode node)
            {
                RenderChildren(node);
            }

            public override void VisitChecksum(ChecksumIRNode node)
            {
                Context.BasicWriter.WriteChecksum(Context, node);
            }

            public override void VisitUsingStatement(UsingStatementIRNode node)
            {
                Context.BasicWriter.WriteUsingStatement(Context, node);
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIRNode node)
            {
                Context.Writer
                    .Write("namespace ")
                    .WriteLine(node.Content);

                using (Context.Writer.BuildScope())
                {
                    Context.Writer.WriteLineHiddenDirective();
                    RenderChildren(node);
                }
            }

            public override void VisitClassDeclaration(ClassDeclarationIRNode node)
            {
                Context.Writer
                    .Write(node.AccessModifier)
                    .Write(" class ")
                    .Write(node.Name);

                if (node.BaseType != null || node.Interfaces != null)
                {
                    Context.Writer.Write(" : ");
                }

                if (node.BaseType != null)
                {
                    Context.Writer.Write(node.BaseType);

                    if (node.Interfaces != null)
                    {
                        Context.Writer.WriteParameterSeparator();
                    }
                }

                if (node.Interfaces != null)
                {
                    for (var i = 0; i < node.Interfaces.Count; i++)
                    {
                        Context.Writer.Write(node.Interfaces[i]);

                        if (i + 1 < node.Interfaces.Count)
                        {
                            Context.Writer.WriteParameterSeparator();
                        }
                    }
                }

                Context.Writer.WriteLine();

                using (Context.Writer.BuildScope())
                {
                    RenderChildren(node);
                }
            }

            public override void VisitMethodDeclaration(MethodDeclarationIRNode node)
            {
                Context.Writer.WriteLine("#pragma warning disable 1998");

                Context.Writer
                    .Write(node.AccessModifier)
                    .Write(" ");

                if (node.Modifiers != null)
                {
                    for (var i = 0; i < node.Modifiers.Count; i++)
                    {
                        Context.Writer.Write(node.Modifiers[i]);

                        if (i + 1 < node.Modifiers.Count)
                        {
                            Context.Writer.Write(" ");
                        }
                    }
                }

                Context.Writer
                    .Write(" ")
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

            public override void VisitFieldDeclaration(FieldDeclarationIRNode node)
            {
                Context.Writer.WriteField(node.AccessModifier, node.Modifiers, node.Type, node.Name);
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationIRNode node)
            {
                Context.Writer.WriteAutoPropertyDeclaration(node.AccessModifier, node.Modifiers, node.Type, node.Name);
            }

            public override void VisitExtension(ExtensionIRNode node)
            {
                node.WriteNode(_target, Context);
            }

            public override void VisitCSharpExpression(CSharpExpressionIRNode node)
            {
                Context.BasicWriter.WriteCSharpExpression(Context, node);
            }

            public override void VisitCSharpStatement(CSharpStatementIRNode node)
            {
                Context.BasicWriter.WriteCSharpStatement(Context, node);
            }

            public override void VisitHtmlAttribute(HtmlAttributeIRNode node)
            {
                Context.BasicWriter.WriteHtmlAttribute(Context, node);
            }

            public override void VisitHtmlAttributeValue(HtmlAttributeValueIRNode node)
            {
                Context.BasicWriter.WriteHtmlAttributeValue(Context, node);
            }

            public override void VisitCSharpAttributeValue(CSharpAttributeValueIRNode node)
            {
                Context.BasicWriter.WriteCSharpAttributeValue(Context, node);
            }

            public override void VisitHtml(HtmlContentIRNode node)
            {
                Context.BasicWriter.WriteHtmlContent(Context, node);
            }

            public override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIRNode node)
            {
                Context.TagHelperWriter.WriteDeclareTagHelperFields(Context, node);
            }

            public override void VisitTagHelper(TagHelperIRNode node)
            {
                using (Context.Push(new TagHelperRenderingContext()))
                {
                    Context.RenderChildren(node);
                }
            }

            public override void VisitInitializeTagHelperStructure(InitializeTagHelperStructureIRNode node)
            {
                Context.TagHelperWriter.WriteInitializeTagHelperStructure(Context, node);
            }

            public override void VisitCreateTagHelper(CreateTagHelperIRNode node)
            {
                Context.TagHelperWriter.WriteCreateTagHelper(Context, node);
            }

            public override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIRNode node)
            {
                Context.TagHelperWriter.WriteAddTagHelperHtmlAttribute(Context, node);
            }

            public override void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
            {
                Context.TagHelperWriter.WriteSetTagHelperProperty(Context, node);
            }

            public override void VisitExecuteTagHelpers(ExecuteTagHelpersIRNode node)
            {
                Context.TagHelperWriter.WriteExecuteTagHelpers(Context, node);
            }

            public override void VisitDefault(RazorIRNode node)
            {
                Context.RenderChildren(node);
            }
        }
    }
}
