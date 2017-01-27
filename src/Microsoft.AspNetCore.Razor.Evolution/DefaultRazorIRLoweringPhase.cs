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

            var builder = RazorIRBuilder.Document();
            var document = (DocumentIRNode)builder.Current;
            var namespaces = new HashSet<string>();

            var i = 0;
            foreach (var namespaceImport in syntaxTree.Options.NamespaceImports)
            {
                if (namespaces.Add(namespaceImport))
                {
                    var @using = new UsingStatementIRNode()
                    {
                        Content = namespaceImport,
                    };

                    builder.Insert(i++, @using);
                }
            }

            var checksum = ChecksumIRNode.Create(codeDocument.Source);
            builder.Insert(0, checksum);

            // The import documents should be inserted logically before the main document.
            var imports = codeDocument.GetImportSyntaxTrees();
            if (imports != null)
            {
                var importsVisitor = new ImportsVisitor(document, builder, namespaces);

                for (var j = 0; j < imports.Count; j++)
                {
                    var import = imports[j];

                    importsVisitor.Filename = import.Source.Filename;
                    importsVisitor.VisitBlock(import.Root);
                }
            }

            var visitor = new MainSourceVisitor(document, builder, namespaces)
            {
                 Filename = syntaxTree.Source.Filename,
            };

            visitor.VisitBlock(syntaxTree.Root);
            
            codeDocument.SetIRDocument(document);
        }

        private class LoweringVisitor : ParserVisitor
        {
            protected readonly RazorIRBuilder _builder;
            protected readonly DocumentIRNode _document;
            protected readonly HashSet<string> _namespaces;

            public LoweringVisitor(DocumentIRNode document, RazorIRBuilder builder, HashSet<string> namespaces)
            {
                _document = document;
                _builder = builder;
                _namespaces = namespaces;
            }

            public string Filename { get; set; }

            public override void VisitImportSpan(AddImportChunkGenerator chunkGenerator, Span span)
            {
                var namespaceImport = chunkGenerator.Namespace.Trim();

                // Track seen namespaces so we don't add duplicates from options.
                if (_namespaces.Add(namespaceImport))
                {
                    _builder.Add(new UsingStatementIRNode()
                    {
                        Content = namespaceImport,
                        Source = BuildSourceSpanFromNode(span),
                    });
                }
            }

            public override void VisitDirectiveToken(DirectiveTokenChunkGenerator chunkGenerator, Span span)
            {
                _builder.Add(new DirectiveTokenIRNode()
                {
                    Content = span.Content,
                    Descriptor = chunkGenerator.Descriptor,
                    Source = BuildSourceSpanFromNode(span),
                });
            }

            public override void VisitStartDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                _builder.Push(new DirectiveIRNode()
                {
                    Name = chunkGenerator.Descriptor.Name,
                    Descriptor = chunkGenerator.Descriptor,
                });
            }

            public override void VisitEndDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                _builder.Pop();
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

            protected SourceSpan BuildSourceSpanFromNode(SyntaxTreeNode node)
            {
                var location = node.Start;
                var sourceRange = new SourceSpan(
                    node.Start.FilePath ?? Filename,
                    node.Start.AbsoluteIndex,
                    node.Start.LineIndex,
                    node.Start.CharacterIndex,
                    node.Length);
                return sourceRange;
            }
        }

        private class ImportsVisitor : LoweringVisitor
        {
            // Imports only supports usings and single-line directives. We only want to include directive tokens
            // when we're inside a single line directive. Also single line directives can't nest which makes
            // this simple.
            private bool _insideLineDirective;

            public ImportsVisitor(DocumentIRNode document, RazorIRBuilder builder, HashSet<string> namespaces)
                : base(document, builder, namespaces)
            {
            }

            public override void VisitDirectiveToken(DirectiveTokenChunkGenerator chunkGenerator, Span span)
            {
                if (_insideLineDirective)
                {
                    base.VisitDirectiveToken(chunkGenerator, span);
                }
            }

            public override void VisitStartDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                if (chunkGenerator.Descriptor.Kind == DirectiveDescriptorKind.SingleLine)
                {
                    _insideLineDirective = true;
                    base.VisitStartDirectiveBlock(chunkGenerator, block);
                }
            }

            public override void VisitEndDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                if (_insideLineDirective)
                {
                    _insideLineDirective = false;
                    base.VisitEndDirectiveBlock(chunkGenerator, block);
                }
            }
        }
        
        private class MainSourceVisitor : LoweringVisitor
        {
            private DeclareTagHelperFieldsIRNode _tagHelperFields;

            public MainSourceVisitor(DocumentIRNode document, RazorIRBuilder builder, HashSet<string> namespaces)
                : base(document, builder, namespaces)
            {
            }

            // Example
            // <input` checked="hello-world @false"`/>
            //  Name=checked
            //  Prefix= checked="
            //  Suffix="
            public override void VisitStartAttributeBlock(AttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                _builder.Push(new HtmlAttributeIRNode()
                {
                    Name = chunkGenerator.Name,
                    Prefix = chunkGenerator.Prefix,
                    Suffix = chunkGenerator.Suffix,
                    Source = BuildSourceSpanFromNode(block),
                });
            }

            public override void VisitEndAttributeBlock(AttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                _builder.Pop();
            }

            // Example
            // <input checked="hello-world `@false`"/>
            //  Prefix= (space)
            //  Children will contain a token for @false.
            public override void VisitStartDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                _builder.Push(new CSharpAttributeValueIRNode()
                {
                    Prefix = chunkGenerator.Prefix,
                    Source = BuildSourceSpanFromNode(block),
                });
            }

            public override void VisitEndDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                _builder.Pop();
            }

            public override void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunkGenerator, Span span)
            {
                _builder.Add(new HtmlAttributeValueIRNode()
                {
                    Prefix = chunkGenerator.Prefix,
                    Content = chunkGenerator.Value,
                    Source = BuildSourceSpanFromNode(span),
                });
            }

            public override void VisitStartTemplateBlock(TemplateBlockChunkGenerator chunkGenerator, Block block)
            {
                _builder.Push(new TemplateIRNode());
            }

            public override void VisitEndTemplateBlock(TemplateBlockChunkGenerator chunkGenerator, Block block)
            {
                var templateNode = _builder.Pop();
                if (templateNode.Children.Count > 0)
                {
                    var sourceRangeStart = templateNode
                        .Children
                        .FirstOrDefault(child => child.Source != null)
                        ?.Source;

                    if (sourceRangeStart != null)
                    {
                        var contentLength = templateNode.Children.Sum(child => child.Source?.Length ?? 0);

                        templateNode.Source = new SourceSpan(
                            sourceRangeStart.Value.FilePath ?? Filename,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength);
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
                _builder.Push(new CSharpExpressionIRNode());
            }

            public override void VisitEndExpressionBlock(ExpressionChunkGenerator chunkGenerator, Block block)
            {
                var expressionNode = _builder.Pop();

                if (expressionNode.Children.Count > 0)
                {
                    var sourceRangeStart = expressionNode
                        .Children
                        .FirstOrDefault(child => child.Source != null)
                        ?.Source;

                    if (sourceRangeStart != null)
                    {
                        var contentLength = expressionNode.Children.Sum(child => child.Source?.Length ?? 0);

                        expressionNode.Source = new SourceSpan(
                            sourceRangeStart.Value.FilePath ?? Filename,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength);
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

                _builder.Add(new CSharpTokenIRNode()
                {
                    Content = span.Content,
                    Source = BuildSourceSpanFromNode(span),
                });
            }

            public override void VisitStatementSpan(StatementChunkGenerator chunkGenerator, Span span)
            {
                _builder.Add(new CSharpStatementIRNode()
                {
                    Content = span.Content,
                    Source = BuildSourceSpanFromNode(span),
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

                var currentChildren = _builder.Current.Children;
                if (currentChildren.Count > 0 && currentChildren[currentChildren.Count - 1] is HtmlContentIRNode)
                {
                    var existingHtmlContent = (HtmlContentIRNode)currentChildren[currentChildren.Count - 1];
                    existingHtmlContent.Content = string.Concat(existingHtmlContent.Content, span.Content);

                    if (existingHtmlContent.Source != null)
                    {
                        var contentLength = existingHtmlContent.Source.Value.Length + span.Content.Length;

                        existingHtmlContent.Source = new SourceSpan(
                            existingHtmlContent.Source.Value.FilePath ?? Filename,
                            existingHtmlContent.Source.Value.AbsoluteIndex,
                            existingHtmlContent.Source.Value.LineIndex,
                            existingHtmlContent.Source.Value.CharacterIndex,
                            contentLength);
                    }
                }
                else
                {
                    _builder.Add(new HtmlContentIRNode()
                    {
                        Content = span.Content,
                        Source = BuildSourceSpanFromNode(span),
                    });
                }
            }

            public override void VisitStartTagHelperBlock(TagHelperChunkGenerator chunkGenerator, Block block)
            {
                var tagHelperBlock = block as TagHelperBlock;
                if (tagHelperBlock == null)
                {
                    return;
                }

                DeclareTagHelperFields(tagHelperBlock);

                _builder.Push(new TagHelperIRNode()
                {
                    Source = BuildSourceSpanFromNode(block)
                });

                var tagName = tagHelperBlock.TagName;
                if (tagHelperBlock.Descriptors.First().Prefix != null)
                {
                    tagName = tagName.Substring(tagHelperBlock.Descriptors.First().Prefix.Length);
                }

                _builder.Push(new InitializeTagHelperStructureIRNode()
                {
                    TagName = tagName,
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

                _builder.Pop(); // Pop InitializeTagHelperStructureIRNode

                AddTagHelperCreation(tagHelperBlock.Descriptors);
                AddTagHelperAttributes(tagHelperBlock.Attributes, tagHelperBlock.Descriptors);
                AddExecuteTagHelpers();

                _builder.Pop(); // Pop TagHelperIRNode
            }

            private void DeclareTagHelperFields(TagHelperBlock block)
            {
                if (_tagHelperFields == null)
                {
                    _tagHelperFields = new DeclareTagHelperFieldsIRNode() { Parent = _document, };
                    _document.Children.Add(_tagHelperFields);
                }

                foreach (var descriptor in block.Descriptors)
                {
                    _tagHelperFields.UsedTagHelperTypeNames.Add(descriptor.TypeName);
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

                    _builder.Add(createTagHelper);
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
                                ValueStyle = attribute.ValueStyle,
                                Source = BuildSourceSpanFromNode(attributeValueNode)
                            };

                            _builder.Push(setTagHelperProperty);
                            attributeValueNode.Accept(this);
                            _builder.Pop();
                        }
                    }
                    else
                    {
                        var addHtmlAttribute = new AddTagHelperHtmlAttributeIRNode()
                        {
                            Name = attribute.Name,
                            ValueStyle = attribute.ValueStyle
                        };

                        _builder.Push(addHtmlAttribute);
                        if (attributeValueNode != null)
                        {
                            attributeValueNode.Accept(this);
                        }
                        _builder.Pop();
                    }
                }
            }

            private void AddExecuteTagHelpers()
            {
                _builder.Add(new ExecuteTagHelpersIRNode());
            }
        }
    }
}
