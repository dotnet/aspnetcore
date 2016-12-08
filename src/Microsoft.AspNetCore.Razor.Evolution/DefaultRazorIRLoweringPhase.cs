// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorIRLoweringPhase : RazorEnginePhaseBase, IRazorIRLoweringPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency(syntaxTree);

            var visitor = new Visitor();

            visitor.VisitBlock(syntaxTree.Root);

            var irDocument = (DocumentIRNode)visitor.Builder.Build();
            codeDocument.SetIRDocument(irDocument);
        }

        private class Visitor : ParserVisitor
        {
            private readonly Stack<RazorIRBuilder> _builders;

            public Visitor()
            {
                _builders = new Stack<RazorIRBuilder>();
                var document = RazorIRBuilder.Document();
                _builders.Push(document);

                Namespace = new NamespaceDeclarationIRNode();
                Builder.Push(Namespace);

                Class = new ClassDeclarationIRNode();
                Builder.Push(Class);

                Method = new RazorMethodDeclarationIRNode();
                Builder.Push(Method);
            }

            public RazorIRBuilder Builder => _builders.Peek();

            public NamespaceDeclarationIRNode Namespace { get; }

            public ClassDeclarationIRNode Class { get; }

            public RazorMethodDeclarationIRNode Method { get; }

            // Example
            // <input` checked="hello-world @false"`/>
            //  Name=checked
            //  Prefix= checked="
            //  Suffix="
            public override void VisitStartAttributeBlock(AttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                Builder.Push(new HtmlAttributeIRNode()
                {
                    Name = chunkGenerator.Name,
                    Prefix = chunkGenerator.Prefix,
                    Suffix = chunkGenerator.Suffix,
                    SourceRange = new MappingLocation(block.Start, block.Length),
                });
            }

            public override void VisitEndAttributeBlock(AttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                Builder.Pop();
            }

            // Example
            // <input checked="hello-world `@false`"/>
            //  Prefix= (space)
            //  Children will contain a token for @false.
            public override void VisitStartDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                Builder.Push(new CSharpAttributeValueIRNode()
                {
                    Prefix = chunkGenerator.Prefix,
                    SourceRange = new MappingLocation(block.Start, block.Length),
                });
            }

            public override void VisitEndDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                Builder.Pop();
            }

            public override void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunkGenerator, Span span)
            {
                Builder.Add(new HtmlAttributeValueIRNode()
                {
                    Prefix = chunkGenerator.Prefix,
                    Content = chunkGenerator.Value,
                    SourceRange = new MappingLocation(span.Start, span.Length),
                });
            }

            public override void VisitStartTemplateBlock(TemplateBlockChunkGenerator chunkGenerator, Block block)
            {
                Builder.Push(new TemplateIRNode());
            }

            public override void VisitEndTemplateBlock(TemplateBlockChunkGenerator chunkGenerator, Block block)
            {
                Builder.Pop();
            }

            // CSharp expressions are broken up into blocks and spans because Razor allows Razor comments
            // inside an expression.
            // Ex:
            //      @DateTime.@*This is a comment*@Now
            //
            // We need to capture this in the IR so that we can give each piece the correct source mappings
            public override void VisitStartExpressionBlock(ExpressionChunkGenerator chunkGenerator, Block block)
            {
                Builder.Push(new CSharpExpressionIRNode());
            }

            public override void VisitEndExpressionBlock(ExpressionChunkGenerator chunkGenerator, Block block)
            {
                var expressionNode = Builder.Pop();

                if (expressionNode.Children.Count > 0)
                {
                    var sourceRangeStart = expressionNode.Children[0].SourceRange;
                    var contentLength = 0;

                    for (var i = 0; i < expressionNode.Children.Count; i++)
                    {
                        contentLength += expressionNode.Children[i].SourceRange.ContentLength;
                    }

                    expressionNode.SourceRange = new MappingLocation(
                        sourceRangeStart.AbsoluteIndex,
                        sourceRangeStart.LineIndex,
                        sourceRangeStart.CharacterIndex,
                        contentLength,
                        sourceRangeStart.FilePath);
                }

            }

            public override void VisitExpressionSpan(ExpressionChunkGenerator chunkGenerator, Span span)
            {
                Builder.Add(new CSharpTokenIRNode()
                {
                    Content = span.Content,
                    SourceRange = new MappingLocation(span.Start, span.Length),
                });
            }

            public override void VisitTypeMemberSpan(TypeMemberChunkGenerator chunkGenerator, Span span)
            {
                var functionsNode = new CSharpStatementIRNode()
                {
                    Content = span.Content,
                    SourceRange = new MappingLocation(span.Start, span.Length),
                    Parent = Class,
                };

                Class.Children.Add(functionsNode);
            }

            public override void VisitStatementSpan(StatementChunkGenerator chunkGenerator, Span span)
            {
                Builder.Add(new CSharpStatementIRNode()
                {
                    Content = span.Content,
                    SourceRange = new MappingLocation(span.Start, span.Length),
                });
            }

            public override void VisitMarkupSpan(MarkupChunkGenerator chunkGenerator, Span span)
            {
                var currentChildren = Builder.Current.Children;
                if (currentChildren.Count > 0 && currentChildren[currentChildren.Count - 1] is HtmlContentIRNode)
                {
                    var existingHtmlContent = (HtmlContentIRNode)currentChildren[currentChildren.Count - 1];
                    existingHtmlContent.Content = string.Concat(existingHtmlContent.Content, span.Content);
                }
                else
                {
                    Builder.Add(new HtmlContentIRNode()
                    {
                        Content = span.Content,
                        SourceRange = new MappingLocation(span.Start, span.Length),
                    });
                }
            }

            public override void VisitImportSpan(AddImportChunkGenerator chunkGenerator, Span span)
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
                    SourceRange = new MappingLocation(span.Start, span.Length),
                };

                Namespace.Children.Insert(i, @using);
            }

            public override void VisitDirectiveToken(DirectiveTokenChunkGenerator chunkGenerator, Span span)
            {
                Builder.Add(new DirectiveTokenIRNode()
                {
                    Content = span.Content,
                    Descriptor = chunkGenerator.Descriptor,
                    SourceRange = new MappingLocation(span.Start, span.Length),
                });
            }

            public override void VisitStartDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                Builder.Push(new DirectiveIRNode()
                {
                    Name = chunkGenerator.Descriptor.Name,
                    Descriptor = chunkGenerator.Descriptor,
                });
            }

            public override void VisitEndDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                Builder.Pop();
            }
        }
    }
}
