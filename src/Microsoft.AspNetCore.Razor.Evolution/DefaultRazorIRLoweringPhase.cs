// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorIRLoweringPhase : RazorEnginePhaseBase, IRazorIRLoweringPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency<RazorSyntaxTree>(syntaxTree);

            var visitor = new Visitor();

            visitor.VisitBlock(syntaxTree.Root);

            var irDocument = (DocumentIRNode)visitor.Builder.Build();
            codeDocument.SetIRDocument(irDocument);
        }

        private class Visitor : ParserVisitor
        {
            public Visitor()
            {
                Builder = RazorIRBuilder.Document();

                Namespace = new NamespaceDeclarationIRNode();
                NamespaceBuilder = RazorIRBuilder.Create(Namespace);
                Builder.Push(Namespace);

                Class = new ClassDeclarationIRNode();
                ClassBuilder = RazorIRBuilder.Create(Class);
                Builder.Push(Class);

                Method = new MethodDeclarationIRNode();
                Builder.Push(Method);
            }

            public RazorIRBuilder Builder { get; }

            public NamespaceDeclarationIRNode Namespace { get; }

            public RazorIRBuilder NamespaceBuilder { get; }

            public ClassDeclarationIRNode Class { get; }

            public RazorIRBuilder ClassBuilder { get; }

            public MethodDeclarationIRNode Method { get; }

            public override void VisitStartAttributeBlock(AttributeBlockChunkGenerator chunk, Block block)
            {
                Builder.Push(new HtmlAttributeIRNode()
                {
                    Name = chunk.Name,
                    Prefix = chunk.Prefix,
                    Suffix = chunk.Prefix,

                    SourceLocation = block.Start,
                });
            }

            public override void VisitEndAttributeBlock(AttributeBlockChunkGenerator chunk, Block block)
            {
                Builder.Pop();
            }

            public override void VisitStartDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunk, Block block)
            {
                Builder.Push(new CSharpAttributeValueIRNode()
                {
                    Prefix = chunk.Prefix,
                    SourceLocation = block.Start,
                });
            }

            public override void VisitEndDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunk, Block block)
            {
                Builder.Pop();
            }

            public override void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunk, Span span)
            {
                Builder.Add(new HtmlAttributeValueIRNode()
                {
                    Prefix = chunk.Prefix,
                    SourceLocation = span.Start,
                });
            }

            public override void VisitStartTemplateBlock(TemplateBlockChunkGenerator chunk, Block block)
            {
                Builder.Push(new TemplateIRNode());
            }

            public override void VisitEndTemplateBlock(TemplateBlockChunkGenerator chunk, Block block)
            {
                Builder.Pop();
            }

            // CSharp expressions are broken up into blocks and spans because Razor allows Razor comments
            // inside an expression.
            // Ex:
            //      @DateTime.@*This is a comment*@Now
            //
            // We need to capture this in the IR so that we can give each piece the correct source mappings
            public override void VisitStartExpressionBlock(ExpressionChunkGenerator chunk, Block block)
            {
                Builder.Push(new CSharpExpressionIRNode()
                {
                    SourceLocation = block.Start,
                });
            }

            public override void VisitEndExpressionBlock(ExpressionChunkGenerator chunk, Block block)
            {
                Builder.Pop();
            }

            public override void VisitExpressionSpan(ExpressionChunkGenerator chunk, Span span)
            {
                Builder.Add(new CSharpTokenIRNode()
                {
                    Content = span.Content,
                    SourceLocation = span.Start,
                });
            }

            public override void VisitStartSectionBlock(SectionChunkGenerator chunk, Block block)
            {
                Builder.Push(new SectionIRNode()
                {
                    Name = chunk.SectionName,
                });
            }

            public override void VisitEndSectionBlock(SectionChunkGenerator chunk, Block block)
            {
                Builder.Pop();
            }

            public override void VisitTypeMemberSpan(TypeMemberChunkGenerator chunk, Span span)
            {
                ClassBuilder.Add(new CSharpStatementIRNode()
                {
                    Content = span.Content,
                    SourceLocation = span.Start,
                });
            }

            public override void VisitAddTagHelperSpan(AddTagHelperChunkGenerator chunk, Span span)
            {
                // Empty for now
            }

            public override void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunk, Span span)
            {
                // Empty for now
            }

            public override void VisitTagHelperPrefixSpan(TagHelperPrefixDirectiveChunkGenerator chunk, Span span)
            {
                // Empty for now
            }

            public override void VisitStatementSpan(StatementChunkGenerator chunk, Span span)
            {
                Builder.Add(new CSharpStatementIRNode()
                {
                    Content = span.Content,
                    SourceLocation = span.Start,
                });
            }

            public override void VisitSetBaseTypeSpan(SetBaseTypeChunkGenerator chunk, Span span)
            {
                Class.BaseType = span.Content;
            }

            public override void VisitMarkupSpan(MarkupChunkGenerator chunk, Span span)
            {
                Builder.Add(new HtmlContentIRNode()
                {
                    Content = span.Content,
                    SourceLocation = span.Start,
                });
            }

            public override void VisitImportSpan(AddImportChunkGenerator chunk, Span span)
            {
                // For prettiness, let's insert the usings before the class declaration.
                var i = 0;
                for (; i < Namespace.Children.Count; i++)
                {
                    if (Namespace.Children[i] is ClassDeclarationIRNode)
                    {
                        break;
                    }
                }

                var @using = new UsingStatementIRNode()
                {
                    Content = span.Content,
                    Parent = Namespace,
                    SourceLocation = span.Start,
                };

                Namespace.Children.Insert(i, @using);
            }
        }
    }
}
