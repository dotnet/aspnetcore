// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

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

            // The import documents should be inserted logically before the main document.
            var imports = codeDocument.GetImportSyntaxTrees();
            var importedUsings = ImportDirectives(document, builder, syntaxTree.Options, imports);

            // Lower the main document, appending after the imported directives.
            //
            // We need to decide up front if this document is a "component" file. This will affect how
            // lowering behaves.
            LoweringVisitor visitor;
            if (FileKinds.IsComponentImport(codeDocument.GetFileKind()) &&
                syntaxTree.Options.FeatureFlags.AllowComponentFileKind)
            {
                visitor = new ComponentImportFileKindVisitor(document, builder, syntaxTree.Options.FeatureFlags)
                {
                    SourceDocument = syntaxTree.Source,
                };

                visitor.Visit(syntaxTree.Root);
            }
            else if (FileKinds.IsComponent(codeDocument.GetFileKind()) &&
                syntaxTree.Options.FeatureFlags.AllowComponentFileKind)
            {
                visitor = new ComponentFileKindVisitor(document, builder, syntaxTree.Options.FeatureFlags)
                {
                    SourceDocument = syntaxTree.Source,
                };

                visitor.Visit(syntaxTree.Root);
            }
            else
            {
                visitor = new LegacyFileKindVisitor(document, builder, tagHelperContext?.Prefix, syntaxTree.Options.FeatureFlags)
                {
                    SourceDocument = syntaxTree.Source,
                };

                visitor.Visit(syntaxTree.Root);
            }

            // 1. Prioritize non-imported usings over imported ones.
            // 2. Don't import usings that already exist in primary document.
            // 3. Allow duplicate usings in primary document (C# warning).
            var usingReferences = new List<UsingReference>(visitor.Usings);
            for (var j = importedUsings.Count - 1; j >= 0; j--)
            {
                if (!usingReferences.Contains(importedUsings[j]))
                {
                    usingReferences.Insert(0, importedUsings[j]);
                }
            }

            // In each lowering piece above, namespaces were tracked. We render them here to ensure every
            // lowering action has a chance to add a source location to a namespace. Ultimately, closest wins.
            var i = 0;
            foreach (var reference in usingReferences)
            {
                var @using = new UsingDirectiveIntermediateNode()
                {
                    Content = reference.Namespace,
                    Source = reference.Source,
                };

                builder.Insert(i++, @using);
            }

            PostProcessImportedDirectives(document);

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

        private IReadOnlyList<UsingReference> ImportDirectives(
            DocumentIntermediateNode document,
            IntermediateNodeBuilder builder,
            RazorParserOptions options,
            IReadOnlyList<RazorSyntaxTree> imports)
        {
            if (imports == null)
            {
                return Array.Empty<UsingReference>();
            }

            var importsVisitor = new ImportsVisitor(document, builder, options.FeatureFlags);
            for (var i = 0; i < imports.Count; i++)
            {
                var import = imports[i];

                importsVisitor.SourceDocument = import.Source;
                importsVisitor.Visit(import.Root);
            }

            return importsVisitor.Usings;
        }

        private void PostProcessImportedDirectives(DocumentIntermediateNode document)
        {
            var directives = document.FindDescendantReferences<DirectiveIntermediateNode>();
            var seenDirectives = new HashSet<DirectiveDescriptor>();
            for (var i = directives.Count - 1; i >= 0; i--)
            {
                var reference = directives[i];
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
                        {
                            if (seenDirective && descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
                            {
                                // This directive has been overridden, it should be removed from the document.
                                break;
                            }

                            continue;
                        }

                    case DirectiveKind.RazorBlock:
                    case DirectiveKind.CodeBlock:
                        {
                            if (descriptor.Usage == DirectiveUsage.FileScopedSinglyOccurring)
                            {
                                // A block directive cannot be imported.
                                document.Diagnostics.Add(
                                    RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported(descriptor.Directive));
                            }

                            break;
                        }

                    default:
                        throw new InvalidOperationException(Resources.FormatUnexpectedDirectiveKind(typeof(DirectiveKind).FullName));
                }

                // Overridden and invalid imported directives make it to here. They should be removed from the document.

                reference.Remove();
            }
        }

        private struct UsingReference : IEquatable<UsingReference>
        {
            public UsingReference(string @namespace, SourceSpan? source)
            {
                Namespace = @namespace;
                Source = source;
            }
            public string Namespace { get; }

            public SourceSpan? Source { get; }

            public override bool Equals(object other)
            {
                if (other is UsingReference reference)
                {
                    return Equals(reference);
                }

                return false;
            }
            public bool Equals(UsingReference other)
            {
                return string.Equals(Namespace, other.Namespace, StringComparison.Ordinal);
            }

            public override int GetHashCode() => Namespace.GetHashCode();
        }

        private class LoweringVisitor : SyntaxWalker
        {
            protected readonly IntermediateNodeBuilder _builder;
            protected readonly DocumentIntermediateNode _document;
            protected readonly List<UsingReference> _usings;
            protected readonly RazorParserFeatureFlags _featureFlags;

            public LoweringVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, RazorParserFeatureFlags featureFlags)
            {
                _document = document;
                _builder = builder;
                _usings = new List<UsingReference>();
                _featureFlags = featureFlags;
            }

            public IReadOnlyList<UsingReference> Usings => _usings;

            public RazorSourceDocument SourceDocument { get; set; }

            public override void VisitRazorDirective(RazorDirectiveSyntax node)
            {
                IntermediateNode directiveNode;
                var descriptor = node.DirectiveDescriptor;

                if (descriptor != null)
                {
                    var diagnostics = node.GetDiagnostics();

                    // This is an extensible directive.
                    if (IsMalformed(diagnostics))
                    {
                        directiveNode = new MalformedDirectiveIntermediateNode()
                        {
                            DirectiveName = descriptor.Directive,
                            Directive = descriptor,
                            Source = BuildSourceSpanFromNode(node),
                        };
                    }
                    else
                    {
                        directiveNode = new DirectiveIntermediateNode()
                        {
                            DirectiveName = descriptor.Directive,
                            Directive = descriptor,
                            Source = BuildSourceSpanFromNode(node),
                        };
                    }

                    for (var i = 0; i < diagnostics.Length; i++)
                    {
                        directiveNode.Diagnostics.Add(diagnostics[i]);
                    }

                    _builder.Push(directiveNode);
                }

                Visit(node.Body);

                if (descriptor != null)
                {
                    _builder.Pop();
                }
            }

            public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
            {
                var context = node.GetSpanContext();
                if (context == null)
                {
                    base.VisitCSharpStatementLiteral(node);
                    return;
                }
                else if (context.ChunkGenerator is DirectiveTokenChunkGenerator tokenChunkGenerator)
                {
                    _builder.Add(new DirectiveTokenIntermediateNode()
                    {
                        Content = node.GetContent(),
                        DirectiveToken = tokenChunkGenerator.Descriptor,
                        Source = BuildSourceSpanFromNode(node),
                    });
                }
                else if (context.ChunkGenerator is AddImportChunkGenerator importChunkGenerator)
                {
                    var namespaceImport = importChunkGenerator.Namespace.Trim();
                    var namespaceSpan = BuildSourceSpanFromNode(node);
                    _usings.Add(new UsingReference(namespaceImport, namespaceSpan));
                }
                else if (context.ChunkGenerator is AddTagHelperChunkGenerator addTagHelperChunkGenerator)
                {
                    IntermediateNode directiveNode;
                    if (IsMalformed(addTagHelperChunkGenerator.Diagnostics))
                    {
                        directiveNode = new MalformedDirectiveIntermediateNode()
                        {
                            DirectiveName = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Directive,
                            Directive = CSharpCodeParser.AddTagHelperDirectiveDescriptor,
                            Source = BuildSourceSpanFromNode(node),
                        };
                    }
                    else
                    {
                        directiveNode = new DirectiveIntermediateNode()
                        {
                            DirectiveName = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Directive,
                            Directive = CSharpCodeParser.AddTagHelperDirectiveDescriptor,
                            Source = BuildSourceSpanFromNode(node),
                        };
                    }

                    for (var i = 0; i < addTagHelperChunkGenerator.Diagnostics.Count; i++)
                    {
                        directiveNode.Diagnostics.Add(addTagHelperChunkGenerator.Diagnostics[i]);
                    }

                    _builder.Push(directiveNode);

                    _builder.Add(new DirectiveTokenIntermediateNode()
                    {
                        Content = addTagHelperChunkGenerator.LookupText,
                        DirectiveToken = CSharpCodeParser.AddTagHelperDirectiveDescriptor.Tokens[0],
                        Source = BuildSourceSpanFromNode(node),
                    });

                    _builder.Pop();
                }
                else if (context.ChunkGenerator is RemoveTagHelperChunkGenerator removeTagHelperChunkGenerator)
                {
                    IntermediateNode directiveNode;
                    if (IsMalformed(removeTagHelperChunkGenerator.Diagnostics))
                    {
                        directiveNode = new MalformedDirectiveIntermediateNode()
                        {
                            DirectiveName = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Directive,
                            Directive = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
                            Source = BuildSourceSpanFromNode(node),
                        };
                    }
                    else
                    {
                        directiveNode = new DirectiveIntermediateNode()
                        {
                            DirectiveName = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Directive,
                            Directive = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
                            Source = BuildSourceSpanFromNode(node),
                        };
                    }

                    for (var i = 0; i < removeTagHelperChunkGenerator.Diagnostics.Count; i++)
                    {
                        directiveNode.Diagnostics.Add(removeTagHelperChunkGenerator.Diagnostics[i]);
                    }

                    _builder.Push(directiveNode);

                    _builder.Add(new DirectiveTokenIntermediateNode()
                    {
                        Content = removeTagHelperChunkGenerator.LookupText,
                        DirectiveToken = CSharpCodeParser.RemoveTagHelperDirectiveDescriptor.Tokens[0],
                        Source = BuildSourceSpanFromNode(node),
                    });

                    _builder.Pop();
                }
                else if (context.ChunkGenerator is TagHelperPrefixDirectiveChunkGenerator tagHelperPrefixChunkGenerator)
                {
                    IntermediateNode directiveNode;
                    if (IsMalformed(tagHelperPrefixChunkGenerator.Diagnostics))
                    {
                        directiveNode = new MalformedDirectiveIntermediateNode()
                        {
                            DirectiveName = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Directive,
                            Directive = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
                            Source = BuildSourceSpanFromNode(node),
                        };
                    }
                    else
                    {
                        directiveNode = new DirectiveIntermediateNode()
                        {
                            DirectiveName = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Directive,
                            Directive = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
                            Source = BuildSourceSpanFromNode(node),
                        };
                    }

                    for (var i = 0; i < tagHelperPrefixChunkGenerator.Diagnostics.Count; i++)
                    {
                        directiveNode.Diagnostics.Add(tagHelperPrefixChunkGenerator.Diagnostics[i]);
                    }

                    _builder.Push(directiveNode);

                    _builder.Add(new DirectiveTokenIntermediateNode()
                    {
                        Content = tagHelperPrefixChunkGenerator.Prefix,
                        DirectiveToken = CSharpCodeParser.TagHelperPrefixDirectiveDescriptor.Tokens[0],
                        Source = BuildSourceSpanFromNode(node),
                    });

                    _builder.Pop();
                }

                base.VisitCSharpStatementLiteral(node);
            }

            protected SourceSpan? BuildSourceSpanFromNode(SyntaxNode node)
            {
                if (node == null)
                {
                    return null;
                }

                return node.GetSourceSpan(SourceDocument);
            }
        }

        // Lowers a document using *html-as-text* and Tag Helpers
        private class LegacyFileKindVisitor : LoweringVisitor
        {
            private readonly HashSet<string> _renderedBoundAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly string _tagHelperPrefix;

            public LegacyFileKindVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, string tagHelperPrefix, RazorParserFeatureFlags featureFlags)
                : base(document, builder, featureFlags)
            {
                _tagHelperPrefix = tagHelperPrefix;
            }

            // Example
            // <input` checked="hello-world @false"`/>
            //  Name=checked
            //  Prefix= checked="
            //  Suffix="
            public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
            {
                var prefixTokens = MergeLiterals(
                    node.NamePrefix?.LiteralTokens,
                    node.Name.LiteralTokens,
                    node.NameSuffix?.LiteralTokens,
                    node.EqualsToken == null ? new SyntaxList<SyntaxToken>() : new SyntaxList<SyntaxToken>(node.EqualsToken),
                    node.ValuePrefix?.LiteralTokens);
                var prefix = (MarkupTextLiteralSyntax)SyntaxFactory.MarkupTextLiteral(prefixTokens).Green.CreateRed(node, node.NamePrefix?.Position ?? node.Name.Position);

                var name = node.Name.GetContent();
                if (name.StartsWith("data-", StringComparison.OrdinalIgnoreCase) &&
                    !_featureFlags.AllowConditionalDataDashAttributes)
                {
                    Visit(prefix);
                    Visit(node.Value);
                    Visit(node.ValueSuffix);
                }
                else
                {
                    if (node.Value != null && node.Value.ChildNodes().All(c => c is MarkupLiteralAttributeValueSyntax))
                    {
                        // We need to do what ConditionalAttributeCollapser used to do.
                        var literalAttributeValueNodes = node.Value.ChildNodes().Cast<MarkupLiteralAttributeValueSyntax>().ToArray();
                        var valueTokens = SyntaxListBuilder<SyntaxToken>.Create();
                        for (var i = 0; i < literalAttributeValueNodes.Length; i++)
                        {
                            var mergedValue = MergeAttributeValue(literalAttributeValueNodes[i]);
                            valueTokens.AddRange(mergedValue.LiteralTokens);
                        }
                        var rewritten = SyntaxFactory.MarkupTextLiteral(valueTokens.ToList());

                        var mergedLiterals = MergeLiterals(prefix?.LiteralTokens, rewritten.LiteralTokens, node.ValueSuffix?.LiteralTokens);
                        var mergedAttribute = SyntaxFactory.MarkupTextLiteral(mergedLiterals).Green.CreateRed(node.Parent, node.Position);
                        Visit(mergedAttribute);
                    }
                    else
                    {
                        _builder.Push(new HtmlAttributeIntermediateNode()
                        {
                            AttributeName = node.Name.GetContent(),
                            Prefix = prefix.GetContent(),
                            Suffix = node.ValueSuffix?.GetContent() ?? string.Empty,
                            Source = BuildSourceSpanFromNode(node),
                        });

                        VisitAttributeValue(node.Value);

                        _builder.Pop();
                    }
                }
            }

            public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
            {
                var name = node.Name.GetContent();
                if (name.StartsWith("data-", StringComparison.OrdinalIgnoreCase) &&
                    !_featureFlags.AllowConditionalDataDashAttributes)
                {
                    base.VisitMarkupMinimizedAttributeBlock(node);
                    return;
                }

                // Minimized attributes are just html content.
                var literals = MergeLiterals(
                    node.NamePrefix?.LiteralTokens,
                    node.Name?.LiteralTokens);
                var literal = SyntaxFactory.MarkupTextLiteral(literals).Green.CreateRed(node.Parent, node.Position);

                Visit(literal);
            }

            // Example
            // <input checked="hello-world `@false`"/>
            //  Prefix= (space)
            //  Children will contain a token for @false.
            public override void VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
            {
                var containsExpression = false;
                var descendantNodes = node.DescendantNodes(n =>
                {
                    // Don't go into sub block. They may contain expressions but we only care about the top level.
                    return !(n.Parent is CSharpCodeBlockSyntax);
                });
                foreach (var child in descendantNodes)
                {
                    if (child is CSharpImplicitExpressionSyntax || child is CSharpExplicitExpressionSyntax)
                    {
                        containsExpression = true;
                    }
                }

                if (containsExpression)
                {
                    _builder.Push(new CSharpExpressionAttributeValueIntermediateNode()
                    {
                        Prefix = node.Prefix?.GetContent() ?? string.Empty,
                        Source = BuildSourceSpanFromNode(node),
                    });
                }
                else
                {
                    _builder.Push(new CSharpCodeAttributeValueIntermediateNode()
                    {
                        Prefix = node.Prefix?.GetContent() ?? string.Empty,
                        Source = BuildSourceSpanFromNode(node),
                    });
                }

                Visit(node.Value);

                _builder.Pop();
            }

            public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
            {
                _builder.Push(new HtmlAttributeValueIntermediateNode()
                {
                    Prefix = node.Prefix?.GetContent() ?? string.Empty,
                    Source = BuildSourceSpanFromNode(node),
                });

                _builder.Add(new LazyIntermediateToken()
                {
                    ContentFactory = () => node.Value?.GetContent() ?? string.Empty,
                    Kind = TokenKind.Html,
                    Source = BuildSourceSpanFromNode(node.Value)
                });

                _builder.Pop();
            }

            public override void VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
            {
                var templateNode = new TemplateIntermediateNode();
                _builder.Push(templateNode);

                base.VisitCSharpTemplateBlock(node);

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
                            sourceRangeStart.Value.FilePath ?? SourceDocument.FilePath,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength,
                            sourceRangeStart.Value.LineCount,
                            sourceRangeStart.Value.EndCharacterIndex);
                    }
                }
            }

            // CSharp expressions are broken up into blocks and spans because Razor allows Razor comments
            // inside an expression.
            // Ex:
            //      @DateTime.@*This is a comment*@Now
            //
            // We need to capture this in the IR so that we can give each piece the correct source mappings
            public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
            {
                if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
                {
                    base.VisitCSharpExplicitExpression(node);
                    return;
                }

                var expressionNode = new CSharpExpressionIntermediateNode();

                _builder.Push(expressionNode);

                base.VisitCSharpExplicitExpression(node);

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
                            sourceRangeStart.Value.FilePath ?? SourceDocument.FilePath,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength,
                            sourceRangeStart.Value.LineCount,
                            sourceRangeStart.Value.EndCharacterIndex);
                    }
                }
            }

            public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
            {
                if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
                {
                    base.VisitCSharpImplicitExpression(node);
                    return;
                }

                var expressionNode = new CSharpExpressionIntermediateNode();

                _builder.Push(expressionNode);

                base.VisitCSharpImplicitExpression(node);

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
                            sourceRangeStart.Value.FilePath ?? SourceDocument.FilePath,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength,
                            sourceRangeStart.Value.LineCount,
                            sourceRangeStart.Value.EndCharacterIndex);
                    }
                }
            }

            public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
            {
                if (_builder.Current is TagHelperHtmlAttributeIntermediateNode)
                {
                    // If we are top level in a tag helper HTML attribute, we want to be rendered as markup.
                    // This case happens for duplicate non-string bound attributes. They would be initially be categorized as
                    // CSharp but since they are duplicate, they should just be markup.
                    var markupLiteral = SyntaxFactory.MarkupTextLiteral(node.LiteralTokens).Green.CreateRed(node.Parent, node.Position);
                    Visit(markupLiteral);
                    return;
                }

                _builder.Add(new LazyIntermediateToken()
                {
                    ContentFactory = () => node.GetContent(),
                    Kind = TokenKind.CSharp,
                    Source = BuildSourceSpanFromNode(node),
                });

                base.VisitCSharpExpressionLiteral(node);
            }

            public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
            {
                var context = node.GetSpanContext();
                if (context == null || context.ChunkGenerator is StatementChunkGenerator)
                {
                    var isAttributeValue = _builder.Current is CSharpCodeAttributeValueIntermediateNode;

                    if (!isAttributeValue)
                    {
                        var statementNode = new CSharpCodeIntermediateNode()
                        {
                            Source = BuildSourceSpanFromNode(node)
                        };
                        _builder.Push(statementNode);
                    }

                    _builder.Add(new LazyIntermediateToken()
                    {
                        ContentFactory = () => node.GetContent(),
                        Kind = TokenKind.CSharp,
                        Source = BuildSourceSpanFromNode(node),
                    });

                    if (!isAttributeValue)
                    {
                        _builder.Pop();
                    }
                }

                base.VisitCSharpStatementLiteral(node);
            }

            public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
            {
                var context = node.GetSpanContext();
                if (context != null && context.ChunkGenerator == SpanChunkGenerator.Null)
                {
                    return;
                }

                if (node.LiteralTokens.Count == 1)
                {
                    var token = node.LiteralTokens[0];
                    if (token != null &&
                        token.Kind == SyntaxKind.Marker &&
                        token.Content.Length == 0)
                    {
                        // We don't want to create IR nodes for marker tokens.
                        return;
                    }
                }

                VisitHtmlContent(node);
            }

            public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
            {
                if (node.IsMarkupTransition)
                {
                    // No need to visit <text> tags.
                    return;
                }

                foreach (var child in node.Children)
                {
                    Visit(child);
                }
            }

            public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
            {
                if (node.IsMarkupTransition)
                {
                    // No need to visit </text> tags.
                    return;
                }

                foreach (var child in node.Children)
                {
                    Visit(child);
                }
            }

            private void VisitHtmlContent(SyntaxNode node)
            {
                if (node == null || (node is SyntaxToken token && token.IsMissing))
                {
                    return;
                }

                var source = BuildSourceSpanFromNode(node);
                var currentChildren = _builder.Current.Children;
                if (currentChildren.Count > 0 && currentChildren[currentChildren.Count - 1] is HtmlContentIntermediateNode)
                {
                    var existingHtmlContent = (HtmlContentIntermediateNode)currentChildren[currentChildren.Count - 1];

                    if (existingHtmlContent.Source == null && source == null)
                    {
                        Combine(existingHtmlContent, node);
                        return;
                    }

                    if (source != null &&
                        existingHtmlContent.Source != null &&
                        existingHtmlContent.Source.Value.FilePath == source.Value.FilePath &&
                        existingHtmlContent.Source.Value.AbsoluteIndex + existingHtmlContent.Source.Value.Length == source.Value.AbsoluteIndex)
                    {
                        Combine(existingHtmlContent, node);
                        return;
                    }
                }

                var contentNode = new HtmlContentIntermediateNode()
                {
                    Source = source
                };
                _builder.Push(contentNode);

                _builder.Add(new LazyIntermediateToken()
                {
                    ContentFactory = () => node.GetContent(),
                    Kind = TokenKind.Html,
                    Source = source,
                });

                _builder.Pop();
            }

            public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
            {
                var info = node.TagHelperInfo;
                var tagName = info.TagName;
                if (_tagHelperPrefix != null)
                {
                    tagName = tagName.Substring(_tagHelperPrefix.Length);
                }

                var tagHelperNode = new TagHelperIntermediateNode()
                {
                    TagName = tagName,
                    TagMode = info.TagMode,
                    Source = BuildSourceSpanFromNode(node)
                };

                foreach (var tagHelper in info.BindingResult.Descriptors)
                {
                    tagHelperNode.TagHelpers.Add(tagHelper);
                }

                _builder.Push(tagHelperNode);

                _builder.Push(new TagHelperBodyIntermediateNode());

                foreach (var item in node.Body)
                {
                    Visit(item);
                }

                _builder.Pop(); // Pop InitializeTagHelperStructureIntermediateNode

                Visit(node.StartTag);

                _builder.Pop(); // Pop TagHelperIntermediateNode

                // No need to visit the end tag because we don't write any IR for it.

                // We don't want to track attributes from a previous tag helper element.
                _renderedBoundAttributeNames.Clear();
            }

            public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
            {
                foreach (var child in node.Attributes)
                {
                    if (child is MarkupTagHelperAttributeSyntax || child is MarkupMinimizedTagHelperAttributeSyntax)
                    {
                        Visit(child);
                    }
                }
            }

            public override void VisitMarkupMinimizedTagHelperAttribute(MarkupMinimizedTagHelperAttributeSyntax node)
            {
                if (!_featureFlags.AllowMinimizedBooleanTagHelperAttributes)
                {
                    // Minimized attributes are not valid for non-boolean bound attributes. TagHelperBlockRewriter
                    // has already logged an error if it was a non-boolean bound attribute; so we can skip.
                    return;
                }

                var element = node.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
                var descriptors = element.TagHelperInfo.BindingResult.Descriptors;
                var attributeName = node.Name.GetContent();
                var associatedDescriptors = descriptors.Where(descriptor =>
                    descriptor.BoundAttributes.Any(attributeDescriptor => TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, attributeDescriptor)));

                if (associatedDescriptors.Any() && _renderedBoundAttributeNames.Add(attributeName))
                {
                    foreach (var associatedDescriptor in associatedDescriptors)
                    {
                        var associatedAttributeDescriptor = associatedDescriptor.BoundAttributes.First(a =>
                        {
                            return TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, a);
                        });

                        var expectsBooleanValue = associatedAttributeDescriptor.ExpectsBooleanValue(attributeName);

                        if (!expectsBooleanValue)
                        {
                            // We do not allow minimized non-boolean bound attributes.
                            return;
                        }

                        var setTagHelperProperty = new TagHelperPropertyIntermediateNode()
                        {
                            AttributeName = attributeName,
                            BoundAttribute = associatedAttributeDescriptor,
                            TagHelper = associatedDescriptor,
                            AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure,
                            Source = null,
                            IsIndexerNameMatch = TagHelperMatchingConventions.SatisfiesBoundAttributeIndexer(attributeName, associatedAttributeDescriptor),
                        };

                        _builder.Add(setTagHelperProperty);
                    }
                }
                else
                {
                    var addHtmlAttribute = new TagHelperHtmlAttributeIntermediateNode()
                    {
                        AttributeName = attributeName,
                        AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure
                    };

                    _builder.Add(addHtmlAttribute);
                }
            }

            public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
            {
                var element = node.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
                var descriptors = element.TagHelperInfo.BindingResult.Descriptors;
                var attributeName = node.Name.GetContent();
                var attributeValueNode = node.Value;
                var associatedDescriptors = descriptors.Where(descriptor =>
                    descriptor.BoundAttributes.Any(attributeDescriptor => TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, attributeDescriptor)));

                if (associatedDescriptors.Any() && _renderedBoundAttributeNames.Add(attributeName))
                {
                    foreach (var associatedDescriptor in associatedDescriptors)
                    {
                        var associatedAttributeDescriptor = associatedDescriptor.BoundAttributes.First(a =>
                        {
                            return TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, a);
                        });

                        var setTagHelperProperty = new TagHelperPropertyIntermediateNode()
                        {
                            AttributeName = attributeName,
                            BoundAttribute = associatedAttributeDescriptor,
                            TagHelper = associatedDescriptor,
                            AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure,
                            Source = BuildSourceSpanFromNode(attributeValueNode),
                            IsIndexerNameMatch = TagHelperMatchingConventions.SatisfiesBoundAttributeIndexer(attributeName, associatedAttributeDescriptor),
                        };

                        _builder.Push(setTagHelperProperty);
                        VisitAttributeValue(attributeValueNode);
                        _builder.Pop();
                    }
                }
                else
                {
                    var addHtmlAttribute = new TagHelperHtmlAttributeIntermediateNode()
                    {
                        AttributeName = attributeName,
                        AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure
                    };

                    _builder.Push(addHtmlAttribute);
                    VisitAttributeValue(attributeValueNode);
                    _builder.Pop();
                }
            }

            private void VisitAttributeValue(SyntaxNode node)
            {
                if (node == null)
                {
                    return;
                }

                IReadOnlyList<SyntaxNode> children = node.ChildNodes();
                var position = node.Position;
                if (children.Count > 0 &&
                    children[0] is MarkupBlockSyntax markupBlock &&
                    markupBlock.Children.Count == 2 &&
                    markupBlock.Children[0] is MarkupTextLiteralSyntax &&
                    markupBlock.Children[1] is MarkupEphemeralTextLiteralSyntax)
                {
                    // This is a special case when we have an attribute like attr="@@foo".
                    // In this case, we want the foo to be written out as HtmlContent and not HtmlAttributeValue.
                    Visit(markupBlock);
                    children = children.Skip(1).ToList();
                    position = children.Count > 0 ? children[0].Position : position;
                }

                if (children.All(c => c is MarkupLiteralAttributeValueSyntax))
                {
                    var literalAttributeValueNodes = children.Cast<MarkupLiteralAttributeValueSyntax>().ToArray();
                    var valueTokens = SyntaxListBuilder<SyntaxToken>.Create();
                    for (var i = 0; i < literalAttributeValueNodes.Length; i++)
                    {
                        var mergedValue = MergeAttributeValue(literalAttributeValueNodes[i]);
                        valueTokens.AddRange(mergedValue.LiteralTokens);
                    }
                    var rewritten = SyntaxFactory.MarkupTextLiteral(valueTokens.ToList()).Green.CreateRed(node.Parent, position);
                    Visit(rewritten);
                }
                else if (children.All(c => c is MarkupTextLiteralSyntax))
                {
                    var builder = SyntaxListBuilder<SyntaxToken>.Create();
                    var markupLiteralArray = children.Cast<MarkupTextLiteralSyntax>();
                    foreach (var literal in markupLiteralArray)
                    {
                        builder.AddRange(literal.LiteralTokens);
                    }
                    var rewritten = SyntaxFactory.MarkupTextLiteral(builder.ToList()).Green.CreateRed(node.Parent, position);
                    Visit(rewritten);
                }
                else if (children.All(c => c is CSharpExpressionLiteralSyntax))
                {
                    var builder = SyntaxListBuilder<SyntaxToken>.Create();
                    var expressionLiteralArray = children.Cast<CSharpExpressionLiteralSyntax>();
                    SpanContext context = null;
                    foreach (var literal in expressionLiteralArray)
                    {
                        context = literal.GetSpanContext();
                        builder.AddRange(literal.LiteralTokens);
                    }
                    var rewritten = SyntaxFactory.CSharpExpressionLiteral(builder.ToList()).Green.CreateRed(node.Parent, position);
                    rewritten = context != null ? rewritten.WithSpanContext(context) : rewritten;
                    Visit(rewritten);
                }
                else
                {
                    Visit(node);
                }
            }

            private MarkupTextLiteralSyntax MergeAttributeValue(MarkupLiteralAttributeValueSyntax node)
            {
                var valueTokens = MergeLiterals(node.Prefix?.LiteralTokens, node.Value?.LiteralTokens);
                var rewritten = node.Prefix?.Update(valueTokens) ?? node.Value?.Update(valueTokens);
                rewritten = (MarkupTextLiteralSyntax)rewritten?.Green.CreateRed(node, node.Position);
                var originalContext = rewritten.GetSpanContext();
                if (originalContext != null)
                {
                    rewritten = rewritten.WithSpanContext(new SpanContext(new MarkupChunkGenerator(), originalContext.EditHandler));
                }

                return rewritten;
            }

            private void Combine(HtmlContentIntermediateNode node, SyntaxNode item)
            {
                node.Children.Add(new LazyIntermediateToken()
                {
                    ContentFactory = () => item.GetContent(),
                    Kind = TokenKind.Html,
                    Source = BuildSourceSpanFromNode(item),
                });

                if (node.Source != null)
                {
                    node.Source = new SourceSpan(
                        node.Source.Value.FilePath,
                        node.Source.Value.AbsoluteIndex,
                        node.Source.Value.LineIndex,
                        node.Source.Value.CharacterIndex,
                        node.Source.Value.Length + item.FullWidth,
                        node.Source.Value.LineCount,
                        node.Source.Value.EndCharacterIndex);
                }
            }

            private SyntaxList<SyntaxToken> MergeLiterals(params SyntaxList<SyntaxToken>?[] literals)
            {
                var builder = SyntaxListBuilder<SyntaxToken>.Create();
                for (var i = 0; i < literals.Length; i++)
                {
                    var literal = literals[i];
                    if (!literal.HasValue)
                    {
                        continue;
                    }

                    builder.AddRange(literal.Value);
                }

                return builder.ToList();
            }
        }

        // Lowers a document using *html-as-nodes* and Components
        private class ComponentFileKindVisitor : LoweringVisitor
        {
            private readonly HashSet<string> _renderedBoundAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public ComponentFileKindVisitor(
                DocumentIntermediateNode document,
                IntermediateNodeBuilder builder,
                RazorParserFeatureFlags featureFlags)
                : base(document, builder, featureFlags)
            {
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                base.DefaultVisit(node);
            }

            public override void VisitMarkupElement(MarkupElementSyntax node)
            {
                if ((node.StartTag != null && node.StartTag.IsMarkupTransition) ||
                    (node.EndTag != null && node.EndTag.IsMarkupTransition))
                {
                    // We don't want to create a node for Markup transitions (<text></text>). Treat their contents as regular markup.
                    // Technically there shouldn't be an end transition without a start transition but just being defensive.
                    base.VisitMarkupElement(node);
                    return;
                }

                var element = new MarkupElementIntermediateNode()
                {
                    Source = BuildSourceSpanFromNode(node),

                    // Could be empty while the tag is being typed in.
                    TagName = node.StartTag?.Name?.Content ?? node.EndTag?.Name?.Content ?? string.Empty,
                };

                if (node.StartTag != null && node.EndTag != null && node.StartTag.IsVoidElement())
                {
                    element.Diagnostics.Add(
                            ComponentDiagnosticFactory.Create_UnexpectedClosingTagForVoidElement(
                                BuildSourceSpanFromNode(node.EndTag), node.EndTag.GetTagNameWithOptionalBang()));
                }
                else if (node.StartTag != null && node.EndTag == null && !node.StartTag.IsVoidElement() && !node.StartTag.IsSelfClosing())
                {
                    element.Diagnostics.Add(
                        ComponentDiagnosticFactory.Create_UnclosedTag(
                            BuildSourceSpanFromNode(node.StartTag), node.StartTag.GetTagNameWithOptionalBang()));
                }
                else if (node.StartTag == null && node.EndTag != null)
                {
                    element.Diagnostics.Add(
                        ComponentDiagnosticFactory.Create_UnexpectedClosingTag(
                            BuildSourceSpanFromNode(node.EndTag), node.EndTag.GetTagNameWithOptionalBang()));
                }

                if (node.StartTag != null && !_document.Options.SuppressPrimaryMethodBody)
                {
                    // We only want this error during the second phase of the two phase compilation.
                    var startTagName = node.StartTag.GetTagNameWithOptionalBang();
                    if (startTagName != null && startTagName.Length > 0 && char.IsUpper(startTagName, 0))
                    {
                        // A markup element that starts with an uppercase character.
                        // It is most likely intended to be a component. Add a warning.
                        element.Diagnostics.Add(
                            ComponentDiagnosticFactory.Create_UnexpectedMarkupElement(startTagName, BuildSourceSpanFromNode(node.StartTag)));
                    }
                }

                _builder.Push(element);

                base.VisitMarkupElement(node);

                _builder.Pop();
            }

            public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
            {
                // We want to skip over the other misc tokens that make up a start tag, and 
                // just process the attributes.
                //
                // Visit the attributes
                foreach (var block in node.Attributes)
                {
                    if (block is MarkupAttributeBlockSyntax attribute)
                    {
                        VisitMarkupAttributeBlock(attribute);
                    }
                    else if (block is MarkupMinimizedAttributeBlockSyntax minimized)
                    {
                        VisitMarkupMinimizedAttributeBlock(minimized);
                    }
                }
            }

            public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
            {
                // We want to skip over the other misc tokens that make up a start tag, and 
                // just process the attributes.
                //
                // Nothing to do here
            }

            // Example
            // <input` checked="hello-world @false"`/>
            //  Name=checked
            //  Prefix= checked="
            //  Suffix="
            public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
            {
                // For now we're using HtmlAttributeIntermediateNode for these so we're still
                // building Prefix and Suffix, even though we don't really use them. If we
                // end up using another node type in the future this can be simplified quite
                // a lot.
                var prefixTokens = MergeLiterals(
                    node.NamePrefix?.LiteralTokens,
                    node.Name.LiteralTokens,
                    node.NameSuffix?.LiteralTokens,
                    node.EqualsToken == null ? new SyntaxList<SyntaxToken>() : new SyntaxList<SyntaxToken>(node.EqualsToken),
                    node.ValuePrefix?.LiteralTokens);
                var prefix = (MarkupTextLiteralSyntax)SyntaxFactory.MarkupTextLiteral(prefixTokens).Green.CreateRed(node, node.NamePrefix?.Position ?? node.Name.Position);

                var name = node.Name.GetContent();
                _builder.Push(new HtmlAttributeIntermediateNode()
                {
                    AttributeName = node.Name.GetContent(),
                    Prefix = prefix.GetContent(),
                    Suffix = node.ValueSuffix?.GetContent() ?? string.Empty,
                    Source = BuildSourceSpanFromNode(node),
                });

                Visit(node.Value);

                _builder.Pop();
            }

            public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
            {
                var prefixTokens = MergeLiterals(node.NamePrefix?.LiteralTokens, node.Name.LiteralTokens);
                var prefix = (MarkupTextLiteralSyntax)SyntaxFactory.MarkupTextLiteral(prefixTokens).Green.CreateRed(node, node.NamePrefix?.Position ?? node.Name.Position);

                var name = node.Name.GetContent();
                _builder.Add(new HtmlAttributeIntermediateNode()
                {
                    AttributeName = node.Name.GetContent(),
                    Prefix = prefix.GetContent(),
                    Suffix = null,
                    Source = BuildSourceSpanFromNode(node),
                });
            }

            // Example
            // <input checked="hello-world `@false`"/>
            //  Prefix= (space)
            //  Children will contain a token for @false.
            public override void VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
            {
                var containsExpression = false;
                var descendantNodes = node.DescendantNodes(n =>
                {
                    // Don't go into sub block. They may contain expressions but we only care about the top level.
                    return !(n.Parent is CSharpCodeBlockSyntax);
                });
                foreach (var child in descendantNodes)
                {
                    if (child is CSharpImplicitExpressionSyntax || child is CSharpExplicitExpressionSyntax)
                    {
                        containsExpression = true;
                    }
                }

                if (containsExpression)
                {
                    _builder.Push(new CSharpExpressionAttributeValueIntermediateNode()
                    {
                        Prefix = node.Prefix?.GetContent() ?? string.Empty,
                        Source = BuildSourceSpanFromNode(node),
                    });
                }
                else
                {
                    _builder.Push(new CSharpCodeAttributeValueIntermediateNode()
                    {
                        Prefix = node.Prefix?.GetContent() ?? string.Empty,
                        Source = BuildSourceSpanFromNode(node),
                    });
                }

                Visit(node.Value);

                _builder.Pop();
            }

            public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
            {
                _builder.Push(new HtmlAttributeValueIntermediateNode()
                {
                    Prefix = node.Prefix?.GetContent() ?? string.Empty,
                    Source = BuildSourceSpanFromNode(node),
                });

                _builder.Add(new LazyIntermediateToken()
                {
                    ContentFactory = () => node.Value?.GetContent() ?? string.Empty,
                    Kind = TokenKind.Html,
                    Source = BuildSourceSpanFromNode(node.Value)
                });

                _builder.Pop();
            }

            public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
            {
                if (_builder.Current is HtmlAttributeIntermediateNode)
                {
                    // This can happen inside a data- attribute 
                    _builder.Push(new HtmlAttributeValueIntermediateNode()
                    {
                        Prefix = string.Empty,
                        Source = BuildSourceSpanFromNode(node),
                    });

                    _builder.Add(new LazyIntermediateToken()
                    {
                        ContentFactory = () => node.GetContent() ?? string.Empty,
                        Kind = TokenKind.Html,
                        Source = BuildSourceSpanFromNode(node),
                    });

                    _builder.Pop();

                    return;
                }

                var context = node.GetSpanContext();
                if (context != null && context.ChunkGenerator == SpanChunkGenerator.Null)
                {
                    return;
                }

                if (node.LiteralTokens.Count == 1)
                {
                    var token = node.LiteralTokens[0];
                    if (token != null &&
                        token.Kind == SyntaxKind.Marker &&
                        token.Content.Length == 0)
                    {
                        // We don't want to create IR nodes for marker tokens.
                        return;
                    }
                }

                // Combine chunks of HTML literal text if possible.
                var source = BuildSourceSpanFromNode(node);
                var currentChildren = _builder.Current.Children;
                if (currentChildren.Count > 0 &&
                    currentChildren[currentChildren.Count - 1] is HtmlContentIntermediateNode existingHtmlContent)
                {
                    if (existingHtmlContent.Source == null && source == null)
                    {
                        Combine(existingHtmlContent, node);
                        return;
                    }

                    if (source != null &&
                        existingHtmlContent.Source != null &&
                        existingHtmlContent.Source.Value.FilePath == source.Value.FilePath &&
                        existingHtmlContent.Source.Value.AbsoluteIndex + existingHtmlContent.Source.Value.Length == source.Value.AbsoluteIndex)
                    {
                        Combine(existingHtmlContent, node);
                        return;
                    }
                }

                _builder.Add(new HtmlContentIntermediateNode()
                {
                    Source = source,
                    Children =
                    {
                        new LazyIntermediateToken()
                        {
                            ContentFactory = () => node.GetContent(),
                            Kind = TokenKind.Html,
                            Source = source,
                        }
                    }
                });
            }

            public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
            {
                // Comments are ignored by components. We skip over anything that appears inside.
            }

            public override void VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
            {
                var templateNode = new TemplateIntermediateNode();
                _builder.Push(templateNode);

                base.VisitCSharpTemplateBlock(node);

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
                            sourceRangeStart.Value.FilePath ?? SourceDocument.FilePath,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength,
                            sourceRangeStart.Value.LineCount,
                            sourceRangeStart.Value.EndCharacterIndex);
                    }
                }
            }

            // CSharp expressions are broken up into blocks and spans because Razor allows Razor comments
            // inside an expression.
            // Ex:
            //      @DateTime.@*This is a comment*@Now
            //
            // We need to capture this in the IR so that we can give each piece the correct source mappings
            public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
            {
                if (_builder.Current is HtmlAttributeIntermediateNode)
                {
                    // This can happen inside a data- attribute
                    _builder.Push(new CSharpExpressionAttributeValueIntermediateNode()
                    {
                        Prefix = string.Empty,
                        Source = this.BuildSourceSpanFromNode(node),
                    });

                    base.VisitCSharpExplicitExpression(node);

                    _builder.Pop();

                    return;
                }

                if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
                {
                    base.VisitCSharpExplicitExpression(node);
                    return;
                }

                var expressionNode = new CSharpExpressionIntermediateNode();

                _builder.Push(expressionNode);

                base.VisitCSharpExplicitExpression(node);

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
                            sourceRangeStart.Value.FilePath ?? SourceDocument.FilePath,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength,
                            sourceRangeStart.Value.LineCount,
                            sourceRangeStart.Value.EndCharacterIndex);
                    }
                }
            }

            public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
            {
                if (_builder.Current is HtmlAttributeIntermediateNode)
                {
                    // This can happen inside a data- attribute
                    _builder.Push(new CSharpExpressionAttributeValueIntermediateNode()
                    {
                        Prefix = string.Empty,
                        Source = this.BuildSourceSpanFromNode(node),
                    });

                    base.VisitCSharpImplicitExpression(node);

                    _builder.Pop();

                    return;
                }

                if (_builder.Current is CSharpExpressionAttributeValueIntermediateNode)
                {
                    base.VisitCSharpImplicitExpression(node);
                    return;
                }

                var expressionNode = new CSharpExpressionIntermediateNode();

                _builder.Push(expressionNode);

                base.VisitCSharpImplicitExpression(node);

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
                            sourceRangeStart.Value.FilePath ?? SourceDocument.FilePath,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength,
                            sourceRangeStart.Value.LineCount,
                            sourceRangeStart.Value.EndCharacterIndex);
                    }
                }
            }

            public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
            {
                if (_builder.Current is TagHelperHtmlAttributeIntermediateNode)
                {
                    // If we are top level in a tag helper HTML attribute, we want to be rendered as markup.
                    // This case happens for duplicate non-string bound attributes. They would be initially be categorized as
                    // CSharp but since they are duplicate, they should just be markup.
                    var markupLiteral = SyntaxFactory.MarkupTextLiteral(node.LiteralTokens).Green.CreateRed(node.Parent, node.Position);
                    Visit(markupLiteral);
                    return;
                }

                _builder.Add(new LazyIntermediateToken()
                {
                    ContentFactory = () => node.GetContent(),
                    Kind = TokenKind.CSharp,
                    Source = BuildSourceSpanFromNode(node),
                });

                base.VisitCSharpExpressionLiteral(node);
            }

            public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
            {
                var context = node.GetSpanContext();
                if (context == null || context.ChunkGenerator is StatementChunkGenerator)
                {
                    var isAttributeValue = _builder.Current is CSharpCodeAttributeValueIntermediateNode;

                    if (!isAttributeValue)
                    {
                        var statementNode = new CSharpCodeIntermediateNode()
                        {
                            Source = BuildSourceSpanFromNode(node)
                        };
                        _builder.Push(statementNode);
                    }

                    _builder.Add(new LazyIntermediateToken()
                    {
                        ContentFactory = () => node.GetContent(),
                        Kind = TokenKind.CSharp,
                        Source = BuildSourceSpanFromNode(node),
                    });

                    if (!isAttributeValue)
                    {
                        _builder.Pop();
                    }
                }

                base.VisitCSharpStatementLiteral(node);
            }

            public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
            {
                var info = node.TagHelperInfo;
                var tagName = info.TagName;
                var tagHelperNode = new TagHelperIntermediateNode()
                {
                    TagName = tagName,
                    TagMode = info.TagMode,
                    Source = BuildSourceSpanFromNode(node)
                };

                foreach (var tagHelper in info.BindingResult.Descriptors)
                {
                    tagHelperNode.TagHelpers.Add(tagHelper);
                }

                _builder.Push(tagHelperNode);

                _builder.Push(new TagHelperBodyIntermediateNode());

                foreach (var item in node.Body)
                {
                    Visit(item);
                }

                _builder.Pop(); // Pop InitializeTagHelperStructureIntermediateNode

                Visit(node.StartTag);

                _builder.Pop(); // Pop TagHelperIntermediateNode

                // No need to visit the end tag because we don't write any IR for it.

                // We don't want to track attributes from a previous tag helper element.
                _renderedBoundAttributeNames.Clear();

                if (node.StartTag != null && node.EndTag != null)
                {
                    var startTagName = node.StartTag.Name?.Content;
                    var endTagName = node.EndTag.Name?.Content;
                    if (!string.Equals(startTagName, endTagName, StringComparison.Ordinal))
                    {
                        // This is most likely a case mismatch in start and end tags. Otherwise the parser wouldn't have grouped them together.
                        // But we can't have case mismatch in start and end tags in components. Add a diagnostic.
                        tagHelperNode.Diagnostics.Add(
                            ComponentDiagnosticFactory.Create_InconsistentStartAndEndTagName(startTagName, endTagName, BuildSourceSpanFromNode(node.EndTag)));
                    }
                }
            }

            public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
            {
                foreach (var child in node.Attributes)
                {
                    if (child is MarkupTagHelperAttributeSyntax ||
                        child is MarkupMinimizedTagHelperAttributeSyntax ||
                        child is MarkupTagHelperDirectiveAttributeSyntax ||
                        child is MarkupMinimizedTagHelperDirectiveAttributeSyntax)
                    {
                        Visit(child);
                    }
                }
            }

            public override void VisitMarkupMinimizedTagHelperAttribute(MarkupMinimizedTagHelperAttributeSyntax node)
            {
                if (!_featureFlags.AllowMinimizedBooleanTagHelperAttributes)
                {
                    // Minimized attributes are not valid for non-boolean bound attributes. TagHelperBlockRewriter
                    // has already logged an error if it was a non-boolean bound attribute; so we can skip.
                    return;
                }

                var element = node.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
                var descriptors = element.TagHelperInfo.BindingResult.Descriptors;
                var attributeName = node.Name.GetContent();
                var associatedDescriptors = descriptors.Where(descriptor =>
                    descriptor.BoundAttributes.Any(attributeDescriptor => TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, attributeDescriptor)));

                if (associatedDescriptors.Any() && _renderedBoundAttributeNames.Add(attributeName))
                {
                    foreach (var associatedDescriptor in associatedDescriptors)
                    {
                        if (TagHelperMatchingConventions.TryGetFirstBoundAttributeMatch(
                            attributeName,
                            associatedDescriptor,
                            out var associatedAttributeDescriptor,
                            out var indexerMatch,
                            out _,
                            out _))
                        {
                            var expectsBooleanValue = associatedAttributeDescriptor.ExpectsBooleanValue(attributeName);

                            if (!expectsBooleanValue)
                            {
                                // We do not allow minimized non-boolean bound attributes.
                                return;
                            }

                            var setTagHelperProperty = new TagHelperPropertyIntermediateNode()
                            {
                                AttributeName = attributeName,
                                BoundAttribute = associatedAttributeDescriptor,
                                TagHelper = associatedDescriptor,
                                AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure,
                                Source = null,
                                IsIndexerNameMatch = indexerMatch,
                            };

                            _builder.Add(setTagHelperProperty);
                        }
                    }
                }
                else
                {
                    var addHtmlAttribute = new TagHelperHtmlAttributeIntermediateNode()
                    {
                        AttributeName = attributeName,
                        AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure
                    };

                    _builder.Add(addHtmlAttribute);
                }
            }

            public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
            {
                if (!_featureFlags.AllowMinimizedBooleanTagHelperAttributes)
                {
                    // Minimized attributes are not valid for non-boolean bound attributes. TagHelperBlockRewriter
                    // has already logged an error if it was a non-boolean bound attribute; so we can skip.
                    return;
                }

                var element = node.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
                var descriptors = element.TagHelperInfo.BindingResult.Descriptors;
                var attributeName = node.FullName;
                var associatedDescriptors = descriptors.Where(descriptor =>
                    descriptor.BoundAttributes.Any(attributeDescriptor => TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, attributeDescriptor)));

                if (associatedDescriptors.Any() && _renderedBoundAttributeNames.Add(attributeName))
                {
                    foreach (var associatedDescriptor in associatedDescriptors)
                    {
                        if (TagHelperMatchingConventions.TryGetFirstBoundAttributeMatch(
                            attributeName,
                            associatedDescriptor,
                            out var associatedAttributeDescriptor,
                            out var indexerMatch,
                            out var parameterMatch,
                            out var associatedAttributeParameterDescriptor))
                        {
                            // Directive attributes should start with '@' unless the descriptors are misconfigured.
                            // In that case, we would have already logged an error.
                            var actualAttributeName = attributeName.StartsWith("@", StringComparison.Ordinal) ? attributeName.Substring(1) : attributeName;

                            IntermediateNode attributeNode;
                            if (parameterMatch &&
                                TagHelperMatchingConventions.TryGetBoundAttributeParameter(actualAttributeName, out var attributeNameWithoutParameter))
                            {
                                var expectsBooleanValue = associatedAttributeParameterDescriptor.IsBooleanProperty;
                                if (!expectsBooleanValue)
                                {
                                    // We do not allow minimized non-boolean bound attributes.
                                    return;
                                }

                                attributeNode = new TagHelperDirectiveAttributeParameterIntermediateNode()
                                {
                                    AttributeName = actualAttributeName,
                                    AttributeNameWithoutParameter = attributeNameWithoutParameter.Value,
                                    OriginalAttributeName = attributeName,
                                    BoundAttributeParameter = associatedAttributeParameterDescriptor,
                                    BoundAttribute = associatedAttributeDescriptor,
                                    TagHelper = associatedDescriptor,
                                    IsIndexerNameMatch = indexerMatch,
                                    AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure,
                                    Source = null,
                                };
                            }
                            else
                            {
                                var expectsBooleanValue = associatedAttributeDescriptor.ExpectsBooleanValue(attributeName);
                                if (!expectsBooleanValue)
                                {
                                    // We do not allow minimized non-boolean bound attributes.
                                    return;
                                }

                                attributeNode = new TagHelperDirectiveAttributeIntermediateNode()
                                {
                                    AttributeName = actualAttributeName,
                                    OriginalAttributeName = attributeName,
                                    BoundAttribute = associatedAttributeDescriptor,
                                    TagHelper = associatedDescriptor,
                                    AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure,
                                    Source = null,
                                    IsIndexerNameMatch = indexerMatch,
                                };
                            }

                            _builder.Add(attributeNode);
                        }
                    }
                }
                else
                {
                    var addHtmlAttribute = new TagHelperHtmlAttributeIntermediateNode()
                    {
                        AttributeName = attributeName,
                        AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure
                    };

                    _builder.Add(addHtmlAttribute);
                }
            }

            public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
            {
                var element = node.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
                var descriptors = element.TagHelperInfo.BindingResult.Descriptors;
                var attributeName = node.Name.GetContent();
                var attributeValueNode = node.Value;
                var associatedDescriptors = descriptors.Where(descriptor =>
                    descriptor.BoundAttributes.Any(attributeDescriptor => TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, attributeDescriptor)));

                if (associatedDescriptors.Any() && _renderedBoundAttributeNames.Add(attributeName))
                {
                    foreach (var associatedDescriptor in associatedDescriptors)
                    {
                        if (TagHelperMatchingConventions.TryGetFirstBoundAttributeMatch(
                            attributeName,
                            associatedDescriptor,
                            out var associatedAttributeDescriptor,
                            out var indexerMatch,
                            out _,
                            out _))
                        {
                            var setTagHelperProperty = new TagHelperPropertyIntermediateNode()
                            {
                                AttributeName = attributeName,
                                BoundAttribute = associatedAttributeDescriptor,
                                TagHelper = associatedDescriptor,
                                AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure,
                                Source = BuildSourceSpanFromNode(attributeValueNode),
                                IsIndexerNameMatch = indexerMatch,
                            };

                            _builder.Push(setTagHelperProperty);
                            VisitAttributeValue(attributeValueNode);
                            _builder.Pop();
                        }
                    }
                }
                else
                {
                    var addHtmlAttribute = new TagHelperHtmlAttributeIntermediateNode()
                    {
                        AttributeName = attributeName,
                        AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure
                    };

                    _builder.Push(addHtmlAttribute);
                    VisitAttributeValue(attributeValueNode);
                    _builder.Pop();
                }
            }

            public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
            {
                var element = node.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
                var descriptors = element.TagHelperInfo.BindingResult.Descriptors;
                var attributeName = node.FullName;
                var attributeValueNode = node.Value;

                var associatedDescriptors = descriptors.Where(descriptor =>
                    descriptor.BoundAttributes.Any(attributeDescriptor => TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, attributeDescriptor)));

                if (associatedDescriptors.Any() && _renderedBoundAttributeNames.Add(attributeName))
                {
                    foreach (var associatedDescriptor in associatedDescriptors)
                    {
                        if (TagHelperMatchingConventions.TryGetFirstBoundAttributeMatch(
                            attributeName,
                            associatedDescriptor,
                            out var associatedAttributeDescriptor,
                            out var indexerMatch,
                            out var parameterMatch,
                            out var associatedAttributeParameterDescriptor))
                        {
                            // Directive attributes should start with '@' unless the descriptors are misconfigured.
                            // In that case, we would have already logged an error.
                            var actualAttributeName = attributeName.StartsWith("@", StringComparison.Ordinal) ? attributeName.Substring(1) : attributeName;

                            IntermediateNode attributeNode;
                            if (parameterMatch &&
                                TagHelperMatchingConventions.TryGetBoundAttributeParameter(actualAttributeName, out var attributeNameWithoutParameter))
                            {
                                attributeNode = new TagHelperDirectiveAttributeParameterIntermediateNode()
                                {
                                    AttributeName = actualAttributeName,
                                    AttributeNameWithoutParameter = attributeNameWithoutParameter.Value,
                                    OriginalAttributeName = attributeName,
                                    BoundAttributeParameter = associatedAttributeParameterDescriptor,
                                    BoundAttribute = associatedAttributeDescriptor,
                                    TagHelper = associatedDescriptor,
                                    IsIndexerNameMatch = indexerMatch,
                                    AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure,
                                    Source = BuildSourceSpanFromNode(attributeValueNode),
                                };
                            }
                            else
                            {
                                attributeNode = new TagHelperDirectiveAttributeIntermediateNode()
                                {
                                    AttributeName = actualAttributeName,
                                    OriginalAttributeName = attributeName,
                                    BoundAttribute = associatedAttributeDescriptor,
                                    TagHelper = associatedDescriptor,
                                    AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure,
                                    Source = BuildSourceSpanFromNode(attributeValueNode),
                                    IsIndexerNameMatch = indexerMatch,
                                };
                            }

                            _builder.Push(attributeNode);
                            VisitAttributeValue(attributeValueNode);
                            _builder.Pop();
                        }
                    }
                }
                else
                {
                    var addHtmlAttribute = new TagHelperHtmlAttributeIntermediateNode()
                    {
                        AttributeName = attributeName,
                        AttributeStructure = node.TagHelperAttributeInfo.AttributeStructure
                    };

                    _builder.Push(addHtmlAttribute);
                    VisitAttributeValue(attributeValueNode);
                    _builder.Pop();
                }
            }

            private void VisitAttributeValue(SyntaxNode node)
            {
                if (node == null)
                {
                    return;
                }

                IReadOnlyList<SyntaxNode> children = node.ChildNodes();
                var position = node.Position;
                if (children.Count > 0 &&
                    children[0] is MarkupBlockSyntax markupBlock &&
                    markupBlock.Children.Count == 2 &&
                    markupBlock.Children[0] is MarkupTextLiteralSyntax &&
                    markupBlock.Children[1] is MarkupEphemeralTextLiteralSyntax)
                {
                    // This is a special case when we have an attribute like attr="@@foo".
                    // In this case, we want the foo to be written out as HtmlContent and not HtmlAttributeValue.
                    Visit(markupBlock);
                    children = children.Skip(1).ToList();
                    position = children.Count > 0 ? children[0].Position : position;
                }

                if (children.All(c => c is MarkupLiteralAttributeValueSyntax))
                {
                    var literalAttributeValueNodes = children.Cast<MarkupLiteralAttributeValueSyntax>().ToArray();
                    var valueTokens = SyntaxListBuilder<SyntaxToken>.Create();
                    for (var i = 0; i < literalAttributeValueNodes.Length; i++)
                    {
                        var mergedValue = MergeAttributeValue(literalAttributeValueNodes[i]);
                        valueTokens.AddRange(mergedValue.LiteralTokens);
                    }
                    var rewritten = SyntaxFactory.MarkupTextLiteral(valueTokens.ToList()).Green.CreateRed(node.Parent, position);
                    Visit(rewritten);
                }
                else if (children.All(c => c is MarkupTextLiteralSyntax))
                {
                    var builder = SyntaxListBuilder<SyntaxToken>.Create();
                    var markupLiteralArray = children.Cast<MarkupTextLiteralSyntax>();
                    foreach (var literal in markupLiteralArray)
                    {
                        builder.AddRange(literal.LiteralTokens);
                    }
                    var rewritten = SyntaxFactory.MarkupTextLiteral(builder.ToList()).Green.CreateRed(node.Parent, position);
                    Visit(rewritten);
                }
                else if (children.All(c => c is CSharpExpressionLiteralSyntax))
                {
                    var builder = SyntaxListBuilder<SyntaxToken>.Create();
                    var expressionLiteralArray = children.Cast<CSharpExpressionLiteralSyntax>();
                    SpanContext context = null;
                    foreach (var literal in expressionLiteralArray)
                    {
                        context = literal.GetSpanContext();
                        builder.AddRange(literal.LiteralTokens);
                    }
                    var rewritten = SyntaxFactory.CSharpExpressionLiteral(builder.ToList()).Green.CreateRed(node.Parent, position);
                    rewritten = context != null ? rewritten.WithSpanContext(context) : rewritten;
                    Visit(rewritten);
                }
                else
                {
                    Visit(node);
                }
            }

            private MarkupTextLiteralSyntax MergeAttributeValue(MarkupLiteralAttributeValueSyntax node)
            {
                var valueTokens = MergeLiterals(node.Prefix?.LiteralTokens, node.Value?.LiteralTokens);
                var rewritten = node.Prefix?.Update(valueTokens) ?? node.Value?.Update(valueTokens);
                rewritten = (MarkupTextLiteralSyntax)rewritten?.Green.CreateRed(node, node.Position);
                var originalContext = rewritten.GetSpanContext();
                if (originalContext != null)
                {
                    rewritten = rewritten.WithSpanContext(new SpanContext(new MarkupChunkGenerator(), originalContext.EditHandler));
                }

                return rewritten;
            }

            private void Combine(HtmlContentIntermediateNode node, SyntaxNode item)
            {
                node.Children.Add(new LazyIntermediateToken()
                {
                    ContentFactory = () => item.GetContent(),
                    Kind = TokenKind.Html,
                    Source = BuildSourceSpanFromNode(item),
                });

                if (node.Source != null)
                {
                    Debug.Assert(node.Source.Value.FilePath != null);

                    node.Source = new SourceSpan(
                        node.Source.Value.FilePath,
                        node.Source.Value.AbsoluteIndex,
                        node.Source.Value.LineIndex,
                        node.Source.Value.CharacterIndex,
                        node.Source.Value.Length + item.FullWidth,
                        node.Source.Value.LineCount,
                        node.Source.Value.EndCharacterIndex);
                }
            }

            private SyntaxList<SyntaxToken> MergeLiterals(params SyntaxList<SyntaxToken>?[] literals)
            {
                var builder = SyntaxListBuilder<SyntaxToken>.Create();
                for (var i = 0; i < literals.Length; i++)
                {
                    var literal = literals[i];
                    if (!literal.HasValue)
                    {
                        continue;
                    }

                    builder.AddRange(literal.Value);
                }

                return builder.ToList();
            }
        }

        private class ComponentImportFileKindVisitor : LoweringVisitor
        {
            public ComponentImportFileKindVisitor(
                DocumentIntermediateNode document,
                IntermediateNodeBuilder builder,
                RazorParserFeatureFlags featureFlags)
                : base(document, builder, featureFlags)
            {
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                base.DefaultVisit(node);
            }

            public override void VisitMarkupElement(MarkupElementSyntax node)
            {
                _document.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

                base.VisitMarkupElement(node);
            }

            public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
            {
                _document.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

                base.VisitMarkupCommentBlock(node);
            }

            public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
            {
                _document.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

                base.VisitMarkupTagHelperElement(node);
            }

            public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
            {
                _document.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

                base.VisitCSharpExplicitExpression(node);
            }

            public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
            {
                // We typically don't want C# in imports files except for directives. But since Razor directive intellisense
                // is tied to C# intellisense during design time, we want to still generate and IR node for implicit expressions.
                // Otherwise, there will be no source mapping when someone types an `@` leading to no intellisense.
                if (node.FirstAncestorOrSelf<SyntaxNode>(n => n is MarkupStartTagSyntax || n is MarkupEndTagSyntax) != null)
                {
                    // We don't care about implicit expresssion in attributes.
                    return;
                }

                var expressionNode = new CSharpExpressionIntermediateNode();

                _builder.Push(expressionNode);

                base.VisitCSharpImplicitExpression(node);

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
                            sourceRangeStart.Value.FilePath ?? SourceDocument.FilePath,
                            sourceRangeStart.Value.AbsoluteIndex,
                            sourceRangeStart.Value.LineIndex,
                            sourceRangeStart.Value.CharacterIndex,
                            contentLength,
                            sourceRangeStart.Value.LineCount,
                            sourceRangeStart.Value.EndCharacterIndex);
                    }
                }

                _document.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(expressionNode.Source));

                base.VisitCSharpImplicitExpression(node);
            }

            public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
            {
                if (node.FirstAncestorOrSelf<SyntaxNode>(n => n is CSharpImplicitExpressionSyntax) == null)
                {
                    // We only care about implicit expressions.
                    return;
                }

                _builder.Add(new LazyIntermediateToken()
                {
                    ContentFactory = () => node.GetContent(),
                    Kind = TokenKind.CSharp,
                    Source = BuildSourceSpanFromNode(node),
                });
            }

            public override void VisitCSharpStatement(CSharpStatementSyntax node)
            {
                _document.Diagnostics.Add(
                    ComponentDiagnosticFactory.Create_UnsupportedComponentImportContent(BuildSourceSpanFromNode(node)));

                base.VisitCSharpStatement(node);
            }
        }

        private class ImportsVisitor : LoweringVisitor
        {
            public ImportsVisitor(DocumentIntermediateNode document, IntermediateNodeBuilder builder, RazorParserFeatureFlags featureFlags)
                : base(document, new ImportBuilder(builder), featureFlags)
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

        private static bool IsMalformed(IEnumerable<RazorDiagnostic> diagnostics)
            => diagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
