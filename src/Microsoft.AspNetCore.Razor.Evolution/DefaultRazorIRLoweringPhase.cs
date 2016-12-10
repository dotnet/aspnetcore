// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorIRLoweringPhase : RazorEnginePhaseBase, IRazorIRLoweringPhase
    {
        private IRazorConfigureParserFeature[] _parserOptionsCallbacks;

        protected override void OnIntialized()
        {
            _parserOptionsCallbacks = Engine.Features.OfType<IRazorConfigureParserFeature>().ToArray();
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency(syntaxTree);

            var visitor = new Visitor(codeDocument, syntaxTree.Options);

            visitor.VisitBlock(syntaxTree.Root);

            var irDocument = (DocumentIRNode)visitor.Builder.Build();
            codeDocument.SetIRDocument(irDocument);
        }

        private class Visitor : ParserVisitor
        {
            private readonly Stack<RazorIRBuilder> _builders;
            private readonly RazorParserOptions _options;
            private readonly RazorCodeDocument _codeDocument;

            public Visitor(RazorCodeDocument codeDocument, RazorParserOptions options)
            {
                _codeDocument = codeDocument;
                _options = options;
                _builders = new Stack<RazorIRBuilder>();
                var document = RazorIRBuilder.Document();
                _builders.Push(document);

                var checksum = ChecksumIRNode.Create(codeDocument.Source);
                Builder.Add(checksum);

                Namespace = new NamespaceDeclarationIRNode();
                Builder.Push(Namespace);

                foreach (var namespaceImport in options.NamespaceImports)
                {
                    var @using = new UsingStatementIRNode()
                    {
                        Content = namespaceImport,
                        Parent = Namespace,
                    };

                    Builder.Add(@using);
                }

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
                    SourceRange = BuildSourceRangeFromNode(block),
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
                    SourceRange = BuildSourceRangeFromNode(block),
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
                    SourceRange = BuildSourceRangeFromNode(span),
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
                    var sourceRangeStart = expressionNode
                        .Children
                        .FirstOrDefault(child => child.SourceRange != null)
                        ?.SourceRange;
                    var contentLength = expressionNode.Children.Sum(child => child.SourceRange?.ContentLength ?? 0);

                    expressionNode.SourceRange = new MappingLocation(
                        sourceRangeStart.AbsoluteIndex,
                        sourceRangeStart.LineIndex,
                        sourceRangeStart.CharacterIndex,
                        contentLength,
                        sourceRangeStart.FilePath ?? _codeDocument.Source.Filename);
                }

            }

            public override void VisitExpressionSpan(ExpressionChunkGenerator chunkGenerator, Span span)
            {
                Builder.Add(new CSharpTokenIRNode()
                {
                    Content = span.Content,
                    SourceRange = BuildSourceRangeFromNode(span),
                });
            }

            public override void VisitStatementSpan(StatementChunkGenerator chunkGenerator, Span span)
            {
                Builder.Add(new CSharpStatementIRNode()
                {
                    Content = span.Content,
                    SourceRange = BuildSourceRangeFromNode(span),
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
                        SourceRange = BuildSourceRangeFromNode(span),
                    });
                }
            }

            public override void VisitImportSpan(AddImportChunkGenerator chunkGenerator, Span span)
            {
                var namespaceImport = chunkGenerator.Namespace.Trim();

                if (_options.NamespaceImports.Contains(namespaceImport, StringComparer.Ordinal))
                {
                    // Already added by default

                    return;
                }

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
                    Content = namespaceImport,
                    Parent = Namespace,
                    SourceRange = BuildSourceRangeFromNode(span),
                };

                Namespace.Children.Insert(i, @using);
            }

            public override void VisitDirectiveToken(DirectiveTokenChunkGenerator chunkGenerator, Span span)
            {
                Builder.Add(new DirectiveTokenIRNode()
                {
                    Content = span.Content,
                    Descriptor = chunkGenerator.Descriptor,
                    SourceRange = BuildSourceRangeFromNode(span),
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

            private MappingLocation BuildSourceRangeFromNode(SyntaxTreeNode node)
            {
                var location = node.Start;
                var sourceRange = new MappingLocation(
                    location.AbsoluteIndex,
                    location.LineIndex,
                    location.CharacterIndex,
                    node.Length,
                    location.FilePath ?? _codeDocument.Source.Filename);

                return sourceRange;
            }
        }
    }
}
