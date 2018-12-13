// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class DefaultRazorIntermediateNodeLoweringPhase : RazorEnginePhaseBase, IRazorIntermediateNodeLoweringPhase
    {
        private IRazorCodeGenerationOptionsFeature _optionsFeature;

        protected override void OnIntialized()
        {
            _optionsFeature = GetRequiredFeature<IRazorCodeGenerationOptionsFeature>();
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDocumentDependency(syntaxTree);

            // This might not have been set if there are no tag helpers.
            var tagHelperContext = codeDocument.GetTagHelperContext();

            var document = new DocumentIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(document);

            document.Options = codeDocument.GetCodeGenerationOptions() ?? _optionsFeature.GetOptions();

            var namespaces = new Dictionary<string, SourceSpan?>(StringComparer.Ordinal);

            // The import documents should be inserted logically before the main document.
            var imports = codeDocument.GetImportSyntaxTrees();
            if (imports != null)
            {
                var importsVisitor = new ImportsVisitor(document, builder, namespaces, syntaxTree.Options.FeatureFlags);

                for (var j = 0; j < imports.Count; j++)
                {
                    var import = imports[j];

                    importsVisitor.FilePath = import.Source.FilePath;
                    importsVisitor.VisitBlock(import.Root);
                }
            }

            var tagHelperPrefix = tagHelperContext?.Prefix;
            var visitor = new MainSourceVisitor(document, builder, namespaces, tagHelperPrefix, syntaxTree.Options.FeatureFlags)
            {
                FilePath = syntaxTree.Source.FilePath,
            };

            visitor.VisitBlock(syntaxTree.Root);

            // In each lowering piece above, namespaces were tracked. We render them here to ensure every
            // lowering action has a chance to add a source location to a namespace. Ultimately, closest wins.

            var i = 0;
            foreach (var @namespace in namespaces)
            {
                var @using = new UsingDirectiveIntermediateNode()
                {
                    Content = @namespace.Key,
                    Source = @namespace.Value,
                };

                builder.Insert(i++, @using);
            }

            ImportDirectives(document);

            // The document should contain all errors that currently exist in the system. This involves
            // adding the errors from the primary and imported syntax trees.
            for (i = 0; i < syntaxTree.Diagnostics.Count; i++)
            {
                document.Diagnostics.Add(syntaxTree.Diagnostics[i]);
            }

            if (imports != null)
            {
                for (i = 0; i < imports.Count; i++)
                {
                    var import = imports[i];
                    for (var j = 0; j < import.Diagnostics.Count; j++)
                    {
                        document.Diagnostics.Add(import.Diagnostics[j]);
                    }
                }
            }

            codeDocument.SetDocumentIntermediateNode(document);
        }

        private void ImportDirectives(DocumentIntermediateNode document)
        {
            var visitor = new DirectiveVisitor();
            visitor.VisitDocument(document);

            var seenDirectives = new HashSet<DirectiveDescriptor>();
            for (var i = visitor.Directives.Count - 1; i >= 0; i--)
            {
                var reference = visitor.Directives[i];
                var directive = (DirectiveIntermediateNode)reference.Node;
                var descriptor = directive.Directive;
                var seenDirective = !seenDirectives.Add(descriptor);

                if (!directive.IsImported())
                {
                    continue;
                }

                switch (descriptor.Kind)
                {
                    case DirectiveKind.SingleLine:
                        if (seenDirective && descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
                        {
                            // This directive has been overridden, it should be removed from the document.

                            break;
                        }

                        continue;
                    case DirectiveKind.RazorBlock:
                    case DirectiveKind.CodeBlock:
                        if (descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
                        {
                            // A block directive cannot be imported.

                            document.Diagnostics.Add(
                                RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported(descriptor.Directive));
                        }
                        break;
                    default:
                        throw new InvalidOperationException(Resources.FormatUnexpectedDirectiveKind(typeof(DirectiveKind).FullName));
                }

                // Overridden and invalid imported directives make it to here. They should be removed from the document.

                reference.Remove();
            }
        }

        private class LoweringVisitor : ParserVisitor
        {
            protected readonly IntermediateNodeBuilder _builder;
            protected readonly DocumentIntermediateNode _document;
            protected readonly Dictionary<string, SourceSpan?> _namespaces;
            protected readonly RazorParserFeatureFlags _featureFlags;

            public LoweringVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, Dictionary<string, SourceSpan?> namespaces, RazorParserFeatureFlags featureFlags)
            {
                _document = document;
                _builder = builder;
                _namespaces = namespaces;
                _featureFlags = featureFlags;
            }

            public string FilePath { get; set; }

            public override void VisitDirectiveToken(DirectiveTokenChunkGenerator chunkGenerator, Span span)
            {
                _builder.Add(new DirectiveTokenIntermediateNode()
                {
                    Content = span.Content,
                    DirectiveToken = chunkGenerator.Descriptor,
                    Source = BuildSourceSpanFromNode(span),
                });
            }

            public override void VisitDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                IntermediateNode directiveNode;
                if (IsMalformed(chunkGenerator.Diagnostics))
                {
                    directiveNode = new MalformedDirectiveIntermediateNode()
                    {
                        DirectiveName = chunkGenerator.Descriptor.Directive,
                        Directive = chunkGenerator.Descriptor,
                        Source = BuildSourceSpanFromNode(block),
                    };
                }
                else
                {
                    directiveNode = new DirectiveIntermediateNode()
                    {
                        DirectiveName = chunkGenerator.Descriptor.Directive,
                        Directive = chunkGenerator.Descriptor,
                        Source = BuildSourceSpanFromNode(block),
                    };
                }

                for (var i = 0; i < chunkGenerator.Diagnostics.Count; i++)
                {
                    directiveNode.Diagnostics.Add(chunkGenerator.Diagnostics[i]);
                }

                _builder.Push(directiveNode);

                VisitDefault(block);

                _builder.Pop();
            }

            public override void VisitImportSpan(AddImportChunkGenerator chunkGenerator, Span span)
            {
                var namespaceImport = chunkGenerator.Namespace.Trim();
                var namespaceSpan = BuildSourceSpanFromNode(span);
                _namespaces[namespaceImport] = namespaceSpan;
            }

            public override void VisitAddTagHelperSpan(AddTagHelperChunkGenerator chunkGenerator, Span span)
            {
                IntermediateNode directiveNode;
                if (IsMalformed(chunkGenerator.Diagnostics))
                {
                    directiveNode = new MalformedDirectiveIntermediateNode()
                    {
                        DirectiveName = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Directive,
                        Directive = CSharpCodeParser.AddTagHelperDirectiveDescriptor,
                        Source = BuildSourceSpanFromNode(span),
                    };
                }
                else
                {
                    directiveNode = new DirectiveIntermediateNode()
                    {
                        DirectiveName = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Directive,
                        Directive = CSharpCodeParser.AddTagHelperDirectiveDescriptor,
                        Source = BuildSourceSpanFromNode(span),
                    };
                }

                for (var i = 0; i < chunkGenerator.Diagnostics.Count; i++)
                {
                    directiveNode.Diagnostics.Add(chunkGenerator.Diagnostics[i]);
                }

                _builder.Push(directiveNode);

                _builder.Add(new DirectiveTokenIntermediateNode()
                {
                    Content = chunkGenerator.LookupText,
                    DirectiveToken = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Tokens.First(),
                    Source = BuildSourceSpanFromNode(span),
                });

                _builder.Pop();
            }

            public override void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunkGenerator, Span span)
            {
                IntermediateNode directiveNode;
                if (IsMalformed(chunkGenerator.Diagnostics))
                {
                    directiveNode = new MalformedDirectiveIntermediateNode()
                    {
                        DirectiveName = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Directive,
                        Directive = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
                        Source = BuildSourceSpanFromNode(span),
                    };
                }
                else
                {
                    directiveNode = new DirectiveIntermediateNode()
                    {
                        DirectiveName = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Directive,
                        Directive = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
                        Source = BuildSourceSpanFromNode(span),
                    };
                }

                for (var i = 0; i < chunkGenerator.Diagnostics.Count; i++)
                {
                    directiveNode.Diagnostics.Add(chunkGenerator.Diagnostics[i]);
                }

                _builder.Push(directiveNode);

                _builder.Add(new DirectiveTokenIntermediateNode()
                {
                    Content = chunkGenerator.LookupText,
                    DirectiveToken = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Tokens.First(),
                    Source = BuildSourceSpanFromNode(span),
                });

                _builder.Pop();
            }

            public override void VisitTagHelperPrefixDirectiveSpan(TagHelperPrefixDirectiveChunkGenerator chunkGenerator, Span span)
            {
                IntermediateNode directiveNode;
                if (IsMalformed(chunkGenerator.Diagnostics))
                {
                    directiveNode = new MalformedDirectiveIntermediateNode()
                    {
                        DirectiveName = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Directive,
                        Directive = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
                        Source = BuildSourceSpanFromNode(span),
                    };
                }
                else
                {
                    directiveNode = new DirectiveIntermediateNode()
                    {
                        DirectiveName = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Directive,
                        Directive = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
                        Source = BuildSourceSpanFromNode(span),
                    };
                }

                for (var i = 0; i < chunkGenerator.Diagnostics.Count; i++)
                {
                    directiveNode.Diagnostics.Add(chunkGenerator.Diagnostics[i]);
                }

                _builder.Push(directiveNode);

                _builder.Add(new DirectiveTokenIntermediateNode()
                {
                    Content = chunkGenerator.Prefix,
                    DirectiveToken = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Tokens.First(),
                    Source = BuildSourceSpanFromNode(span),
                });

                _builder.Pop();
            }

            protected SourceSpan? BuildSourceSpanFromNode(SyntaxTreeNode node)
            {
                if (node == null || node.Start == SourceLocation.Undefined)
                {
                    return null;
                }

                var span = new SourceSpan(
                    node.Start.FilePath ?? FilePath,
                    node.Start.AbsoluteIndex,
                    node.Start.LineIndex,
                    node.Start.CharacterIndex,
                    node.Length);
                return span;
            }
        }

        private class MainSourceVisitor : LoweringVisitor
        {
            private readonly string _tagHelperPrefix;

            public MainSourceVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, Dictionary<string, SourceSpan?> namespaces, string tagHelperPrefix, RazorParserFeatureFlags featureFlags)
                : base(document, builder, namespaces, featureFlags)
            {
                _tagHelperPrefix = tagHelperPrefix;
            }

            // Example
            // <input` checked="hello-world @false"`/>
            //  Name=checked
            //  Prefix= checked="
            //  Suffix="
            public override void VisitAttributeBlock(AttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                _builder.Push(new HtmlAttributeIntermediateNode()
                {
                    AttributeName = chunkGenerator.Name,
                    Prefix = chunkGenerator.Prefix,
                    Suffix = chunkGenerator.Suffix,
                    Source = BuildSourceSpanFromNode(block),
                });

                VisitDefault(block);

                _builder.Pop();
            }

            // Example
            // <input checked="hello-world `@false`"/>
            //  Prefix= (space)
            //  Children will contain a token for @false.
            public override void VisitDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunkGenerator, Block block)
            {
                var firstChild = block.Children.FirstOrDefault(c => c.IsBlock) as Block;
                if (firstChild == null || firstChild.Type == BlockKindInternal.Expression)
                {
                    _builder.Push(new CSharpExpressionAttributeValueIntermediateNode()
                    {
                        Prefix = chunkGenerator.Prefix,
                        Source = BuildSourceSpanFromNode(block),
                    });
                }
                else
                {
                    _builder.Push(new CSharpCodeAttributeValueIntermediateNode()
                    {
                        Prefix = chunkGenerator.Prefix,
                        Source = BuildSourceSpanFromNode(block),
                    });
                }

                VisitDefault(block);

                _builder.Pop();
            }

            public override void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunkGenerator, Span span)
            {
                _builder.Push(new HtmlAttributeValueIntermediateNode()
                {
                    Prefix = chunkGenerator.Prefix,
                    Source = BuildSourceSpanFromNode(span),
                });

                var location = chunkGenerator.Value.Location;
                SourceSpan? valueSpan = null;
                if (location != SourceLocation.Undefined)
                {
                    valueSpan = new SourceSpan(
                        location.FilePath ?? FilePath,
                        location.AbsoluteIndex,
                        location.LineIndex,
                        location.CharacterIndex,
                        chunkGenerator.Value.Value.Length);
                }

                _builder.Add(new IntermediateToken()
                {
                    Content = chunkGenerator.Value,
                    Kind = TokenKind.Html,
                    Source = valueSpan
                });

                _builder.Pop();
            }

            public override void VisitTemplateBlock(TemplateBlockChunkGenerator chunkGenerator, Block block)
            {
                var templateNode = new TemplateIntermediateNode();
                _builder.Push(templateNode);

                VisitDefault(block);

                _builder.Pop();

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
                            sourceRangeStart.Value.FilePath ?? FilePath,
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
            public override void VisitExpressionBlock(ExpressionChunkGenerator chunkGenerator, Block block)
            {
                if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
                {
                    VisitDefault(block);
                    return;
                }

                var expressionNode = new CSharpExpressionIntermediateNode();

                _builder.Push(expressionNode);

                VisitDefault(block);

                _builder.Pop();

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
                            sourceRangeStart.Value.FilePath ?? FilePath,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength);
                    }
                }
            }

            public override void VisitExpressionSpan(ExpressionChunkGenerator chunkGenerator, Span span)
            {
                _builder.Add(new IntermediateToken()
                {
                    Content = span.Content,
                    Kind = TokenKind.CSharp,
                    Source = BuildSourceSpanFromNode(span),
                });
            }

            public override void VisitStatementSpan(StatementChunkGenerator chunkGenerator, Span span)
            {
                var isAttributeValue = _builder.Current is CSharpCodeAttributeValueIntermediateNode;

                if (!isAttributeValue)
                {
                    var statementNode = new CSharpCodeIntermediateNode()
                    {
                        Source = BuildSourceSpanFromNode(span)
                    };
                    _builder.Push(statementNode);
                }

                _builder.Add(new IntermediateToken()
                {
                    Content = span.Content,
                    Kind = TokenKind.CSharp,
                    Source = BuildSourceSpanFromNode(span),
                });

                if (!isAttributeValue)
                {
                    _builder.Pop();
                }
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

                var source = BuildSourceSpanFromNode(span);
                var currentChildren = _builder.Current.Children;
                if (currentChildren.Count > 0 && currentChildren[currentChildren.Count - 1] is HtmlContentIntermediateNode)
                {
                    var existingHtmlContent = (HtmlContentIntermediateNode)currentChildren[currentChildren.Count - 1];

                    if (existingHtmlContent.Source == null && source == null)
                    {
                        Combine(existingHtmlContent, span);
                        return;
                    }

                    if (source != null &&
                        existingHtmlContent.Source != null &&
                        existingHtmlContent.Source.Value.FilePath == source.Value.FilePath &&
                        existingHtmlContent.Source.Value.AbsoluteIndex + existingHtmlContent.Source.Value.Length == source.Value.AbsoluteIndex)
                    {
                        Combine(existingHtmlContent, span);
                        return;
                    }
                }

                var contentNode = new HtmlContentIntermediateNode()
                {
                    Source = source
                };
                _builder.Push(contentNode);

                _builder.Add(new IntermediateToken()
                {
                    Content = span.Content,
                    Kind = TokenKind.Html,
                    Source = source,
                });

                _builder.Pop();
            }

            public override void VisitTagHelperBlock(TagHelperChunkGenerator chunkGenerator, Block block)
            {
                var tagHelperBlock = block as TagHelperBlock;
                if (tagHelperBlock == null)
                {
                    return;
                }

                var tagName = tagHelperBlock.TagName;
                if (_tagHelperPrefix != null)
                {
                    tagName = tagName.Substring(_tagHelperPrefix.Length);
                }

                var tagHelperNode = new TagHelperIntermediateNode()
                {
                    TagName = tagName,
                    TagMode = tagHelperBlock.TagMode,
                    Source = BuildSourceSpanFromNode(block)
                };

                foreach (var tagHelper in tagHelperBlock.Binding.Descriptors)
                {
                    tagHelperNode.TagHelpers.Add(tagHelper);
                }

                _builder.Push(tagHelperNode);

                _builder.Push(new TagHelperBodyIntermediateNode());

                VisitDefault(block);

                _builder.Pop(); // Pop InitializeTagHelperStructureIntermediateNode

                AddTagHelperAttributes(tagHelperBlock.Attributes, tagHelperBlock.Binding);

                _builder.Pop(); // Pop TagHelperIntermediateNode
            }

            private void Combine(HtmlContentIntermediateNode node, Span span)
            {
                node.Children.Add(new IntermediateToken()
                {
                    Content = span.Content,
                    Kind = TokenKind.Html,
                    Source = BuildSourceSpanFromNode(span),
                });

                if (node.Source != null)
                {
                    Debug.Assert(node.Source.Value.FilePath != null);

                    node.Source = new SourceSpan(
                        node.Source.Value.FilePath,
                        node.Source.Value.AbsoluteIndex,
                        node.Source.Value.LineIndex,
                        node.Source.Value.CharacterIndex,
                        node.Source.Value.Length + span.Content.Length);
                }
            }

            private void AddTagHelperAttributes(IList<TagHelperAttributeNode> attributes, TagHelperBinding tagHelperBinding)
            {
                var descriptors = tagHelperBinding.Descriptors;
                var renderedBoundAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var attribute in attributes)
                {
                    var attributeValueNode = attribute.Value;
                    var associatedDescriptors = descriptors.Where(descriptor =>
                        descriptor.BoundAttributes.Any(attributeDescriptor => TagHelperMatchingConventions.CanSatisfyBoundAttribute(attribute.Name, attributeDescriptor)));

                    if (associatedDescriptors.Any() && renderedBoundAttributeNames.Add(attribute.Name))
                    {
                        var isMinimizedAttribute = attributeValueNode == null;
                        if (isMinimizedAttribute && !_featureFlags.AllowMinimizedBooleanTagHelperAttributes)
                        {
                            // Minimized attributes are not valid for non-boolean bound attributes. TagHelperBlockRewriter
                            // has already logged an error if it was a non-boolean bound attribute; so we can skip.
                            continue;
                        }

                        foreach (var associatedDescriptor in associatedDescriptors)
                        {
                            var associatedAttributeDescriptor = associatedDescriptor.BoundAttributes.First(a =>
                            {
                                return TagHelperMatchingConventions.CanSatisfyBoundAttribute(attribute.Name, a);
                            });

                            var expectsBooleanValue = associatedAttributeDescriptor.ExpectsBooleanValue(attribute.Name);

                            if (isMinimizedAttribute && !expectsBooleanValue)
                            {
                                // We do not allow minimized non-boolean bound attributes.
                                continue;
                            }

                            var setTagHelperProperty = new TagHelperPropertyIntermediateNode()
                            {
                                AttributeName = attribute.Name,
                                BoundAttribute = associatedAttributeDescriptor,
                                TagHelper = associatedDescriptor,
                                AttributeStructure = attribute.AttributeStructure,
                                Source = BuildSourceSpanFromNode(attributeValueNode),
                                IsIndexerNameMatch = TagHelperMatchingConventions.SatisfiesBoundAttributeIndexer(attribute.Name, associatedAttributeDescriptor),
                            };

                            _builder.Push(setTagHelperProperty);
                            attributeValueNode?.Accept(this);
                            _builder.Pop();
                        }
                    }
                    else
                    {
                        var addHtmlAttribute = new TagHelperHtmlAttributeIntermediateNode()
                        {
                            AttributeName = attribute.Name,
                            AttributeStructure = attribute.AttributeStructure
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
        }

        private class ImportsVisitor : LoweringVisitor
        {
            public ImportsVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, Dictionary<string, SourceSpan?> namespaces, RazorParserFeatureFlags featureFlags)
                : base(document, new ImportBuilder(builder), namespaces, featureFlags)
            {
            }

            private class ImportBuilder : IntermediateNodeBuilder
            {
                private readonly IntermediateNodeBuilder _innerBuilder;

                public ImportBuilder(IntermediateNodeBuilder innerBuilder)
                {
                    _innerBuilder = innerBuilder;
                }

                public override IntermediateNode Current => _innerBuilder.Current;

                public override void Add(IntermediateNode node)
                {
                    node.Annotations[CommonAnnotations.Imported] = CommonAnnotations.Imported;
                    _innerBuilder.Add(node);
                }

                public override IntermediateNode Build() => _innerBuilder.Build();

                public override void Insert(int index, IntermediateNode node)
                {
                    node.Annotations[CommonAnnotations.Imported] = CommonAnnotations.Imported;
                    _innerBuilder.Insert(index, node);
                }

                public override IntermediateNode Pop() => _innerBuilder.Pop();

                public override void Push(IntermediateNode node)
                {
                    node.Annotations[CommonAnnotations.Imported] = CommonAnnotations.Imported;
                    _innerBuilder.Push(node);
                }
            }
        }

        private class DirectiveVisitor : IntermediateNodeWalker
        {
            public List<IntermediateNodeReference> Directives = new List<IntermediateNodeReference>();

            public override void VisitDirective(DirectiveIntermediateNode node)
            {
                Directives.Add(new IntermediateNodeReference(Parent, node));

                base.VisitDirective(node);
            }
        }

        private static bool IsMalformed(List<RazorDiagnostic> diagnostics)
            => diagnostics.Count > 0 && diagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
