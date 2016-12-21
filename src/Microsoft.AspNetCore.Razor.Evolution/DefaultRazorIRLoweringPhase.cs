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
                var templateNode = Builder.Pop();
                if (templateNode.Children.Count > 0)
                {
                    var sourceRangeStart = templateNode
                        .Children
                        .FirstOrDefault(child => child.SourceRange != null)
                        ?.SourceRange;

                    if (sourceRangeStart != null)
                    {
                        var contentLength = templateNode.Children.Sum(child => child.SourceRange?.ContentLength ?? 0);

                        templateNode.SourceRange = new MappingLocation(
                            sourceRangeStart.AbsoluteIndex,
                            sourceRangeStart.LineIndex,
                            sourceRangeStart.CharacterIndex,
                            contentLength,
                            sourceRangeStart.FilePath ?? _codeDocument.Source.Filename);
                    }
                }
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

                    if (sourceRangeStart != null)
                    {
                        var contentLength = expressionNode.Children.Sum(child => child.SourceRange?.ContentLength ?? 0);

                        expressionNode.SourceRange = new MappingLocation(
                            sourceRangeStart.AbsoluteIndex,
                            sourceRangeStart.LineIndex,
                            sourceRangeStart.CharacterIndex,
                            contentLength,
                            sourceRangeStart.FilePath ?? _codeDocument.Source.Filename);
                    }
                }
            }

            public override void VisitExpressionSpan(ExpressionChunkGenerator chunkGenerator, Span span)
            {
                if (span.Symbols.Count == 1)
                {
                    var symbol = span.Symbols[0] as CSharpSymbol;
                    if (symbol != null &&
                        symbol.Type == CSharpSymbolType.Unknown &&
                        symbol.Content.Length == 0)
                    {
                        // We don't want to create IR nodes for marker symbols.
                        return;
                    }
                }

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
                if (span.Symbols.Count == 1)
                {
                    var symbol = span.Symbols[0] as HtmlSymbol;
                    if (symbol != null &&
                        symbol.Type == HtmlSymbolType.Unknown &&
                        symbol.Content.Length == 0)
                    {
                        // We don't want to create IR nodes for marker symbols.
                        return;
                    }
                }

                var currentChildren = Builder.Current.Children;
                if (currentChildren.Count > 0 && currentChildren[currentChildren.Count - 1] is HtmlContentIRNode)
                {
                    var existingHtmlContent = (HtmlContentIRNode)currentChildren[currentChildren.Count - 1];
                    existingHtmlContent.Content = string.Concat(existingHtmlContent.Content, span.Content);
                    existingHtmlContent.SourceRange = new MappingLocation(
                        existingHtmlContent.SourceRange.AbsoluteIndex,
                        existingHtmlContent.SourceRange.LineIndex,
                        existingHtmlContent.SourceRange.CharacterIndex,
                        existingHtmlContent.SourceRange.ContentLength + span.Content.Length,
                        existingHtmlContent.SourceRange.FilePath);
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

            public override void VisitStartTagHelperBlock(TagHelperChunkGenerator chunkGenerator, Block block)
            {
                var tagHelperBlock = block as TagHelperBlock;
                if (tagHelperBlock == null)
                {
                    return;
                }

                DeclareTagHelperFields(tagHelperBlock);

                Builder.Push(new TagHelperIRNode());

                Builder.Push(new InitializeTagHelperStructureIRNode()
                {
                    TagName = tagHelperBlock.TagName,
                    TagMode = tagHelperBlock.TagMode
                });
            }

            public override void VisitEndTagHelperBlock(TagHelperChunkGenerator chunkGenerator, Block block)
            {
                var tagHelperBlock = block as TagHelperBlock;
                if (tagHelperBlock == null)
                {
                    return;
                }

                Builder.Pop(); // Pop InitializeTagHelperStructureIRNode

                AddTagHelperCreation(tagHelperBlock.Descriptors);
                AddTagHelperAttributes(tagHelperBlock.Attributes, tagHelperBlock.Descriptors);
                AddExecuteTagHelpers();

                Builder.Pop(); // Pop TagHelperIRNode
            }

            public override void VisitAddTagHelperSpan(AddTagHelperChunkGenerator chunkGenerator, Span span)
            {
            }

            public override void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunkGenerator, Span span)
            {
            }

            public override void VisitTagHelperPrefixDirectiveSpan(TagHelperPrefixDirectiveChunkGenerator chunkGenerator, Span span)
            {
            }

            private void DeclareTagHelperFields(TagHelperBlock block)
            {
                var declareFieldsNode = Class.Children.OfType<DeclareTagHelperFieldsIRNode>().SingleOrDefault();
                if (declareFieldsNode == null)
                {
                    declareFieldsNode = new DeclareTagHelperFieldsIRNode();
                    declareFieldsNode.Parent = Class;

                    var methodIndex = Class.Children.IndexOf(Method);
                    Class.Children.Insert(methodIndex, declareFieldsNode);
                }

                foreach (var descriptor in block.Descriptors)
                {
                    declareFieldsNode.UsedTagHelperTypeNames.Add(descriptor.TypeName);
                }
            }

            private void AddTagHelperCreation(IEnumerable<TagHelperDescriptor> descriptors)
            {
                foreach (var descriptor in descriptors)
                {
                    var createTagHelper = new CreateTagHelperIRNode()
                    {
                        TagHelperTypeName = descriptor.TypeName,
                        Descriptor = descriptor
                    };

                    Builder.Add(createTagHelper);
                }
            }

            private void AddTagHelperAttributes(IList<TagHelperAttributeNode> attributes, IEnumerable<TagHelperDescriptor> descriptors)
            {
                var renderedBoundAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var attribute in attributes)
                {
                    var attributeValueNode = attribute.Value;
                    var associatedDescriptors = descriptors.Where(descriptor =>
                        descriptor.Attributes.Any(attributeDescriptor => attributeDescriptor.IsNameMatch(attribute.Name)));

                    if (associatedDescriptors.Any() && renderedBoundAttributeNames.Add(attribute.Name))
                    {
                        if (attributeValueNode == null)
                        {
                            // Minimized attributes are not valid for bound attributes. TagHelperBlockRewriter has already
                            // logged an error if it was a bound attribute; so we can skip.
                            continue;
                        }

                        foreach (var associatedDescriptor in associatedDescriptors)
                        {
                            var associatedAttributeDescriptor = associatedDescriptor.Attributes.First(
                                attributeDescriptor => attributeDescriptor.IsNameMatch(attribute.Name));
                            var setTagHelperProperty = new SetTagHelperPropertyIRNode()
                            {
                                PropertyName = associatedAttributeDescriptor.PropertyName,
                                AttributeName = attribute.Name,
                                TagHelperTypeName = associatedDescriptor.TypeName,
                                Descriptor = associatedAttributeDescriptor,
                                ValueStyle = attribute.ValueStyle
                            };

                            Builder.Push(setTagHelperProperty);
                            attributeValueNode.Accept(this);
                            Builder.Pop();
                        }
                    }
                    else
                    {
                        var addHtmlAttribute = new AddTagHelperHtmlAttributeIRNode()
                        {
                            Name = attribute.Name,
                            ValueStyle = attribute.ValueStyle
                        };

                        Builder.Push(addHtmlAttribute);
                        if (attributeValueNode != null)
                        {
                            attributeValueNode.Accept(this);
                        }
                        Builder.Pop();
                    }
                }
            }

            private void AddExecuteTagHelpers()
            {
                Builder.Add(new ExecuteTagHelpersIRNode());
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
