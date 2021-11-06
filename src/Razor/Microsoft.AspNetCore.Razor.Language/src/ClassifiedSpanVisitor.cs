// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

internal class ClassifiedSpanVisitor : SyntaxWalker
{
    private readonly RazorSourceDocument _source;
    private readonly List<ClassifiedSpanInternal> _spans;
    private BlockKindInternal _currentBlockKind;
    private SyntaxNode _currentBlock;

    public ClassifiedSpanVisitor(RazorSourceDocument source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        _source = source;
        _spans = new List<ClassifiedSpanInternal>();
        _currentBlockKind = BlockKindInternal.Markup;
    }

    public IReadOnlyList<ClassifiedSpanInternal> ClassifiedSpans => _spans;

    public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Comment, razorCommentSyntax =>
        {
            WriteSpan(razorCommentSyntax.StartCommentTransition, SpanKindInternal.Transition, AcceptedCharactersInternal.None);
            WriteSpan(razorCommentSyntax.StartCommentStar, SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);

            var comment = razorCommentSyntax.Comment;
            if (comment.IsMissing)
            {
                    // We need to generate a classified span at this position. So insert a marker in its place.
                    comment = (SyntaxToken)SyntaxFactory.Token(SyntaxKind.Marker, string.Empty).Green.CreateRed(razorCommentSyntax, razorCommentSyntax.StartCommentStar.EndPosition);
            }
            WriteSpan(comment, SpanKindInternal.Comment, AcceptedCharactersInternal.Any);

            WriteSpan(razorCommentSyntax.EndCommentStar, SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);
            WriteSpan(razorCommentSyntax.EndCommentTransition, SpanKindInternal.Transition, AcceptedCharactersInternal.None);
        });
    }

    public override void VisitCSharpCodeBlock(CSharpCodeBlockSyntax node)
    {
        if (node.Parent is CSharpStatementBodySyntax ||
            node.Parent is CSharpExplicitExpressionBodySyntax ||
            node.Parent is CSharpImplicitExpressionBodySyntax ||
            node.Parent is RazorDirectiveBodySyntax ||
            (_currentBlockKind == BlockKindInternal.Directive &&
            node.Children.Count == 1 &&
            node.Children[0] is CSharpStatementLiteralSyntax))
        {
            base.VisitCSharpCodeBlock(node);
            return;
        }

        WriteBlock(node, BlockKindInternal.Statement, base.VisitCSharpCodeBlock);
    }

    public override void VisitCSharpStatement(CSharpStatementSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Statement, base.VisitCSharpStatement);
    }

    public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Expression, base.VisitCSharpExplicitExpression);
    }

    public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Expression, base.VisitCSharpImplicitExpression);
    }

    public override void VisitRazorDirective(RazorDirectiveSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Directive, base.VisitRazorDirective);
    }

    public override void VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Template, base.VisitCSharpTemplateBlock);
    }

    public override void VisitMarkupBlock(MarkupBlockSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Markup, base.VisitMarkupBlock);
    }

    public override void VisitMarkupTagHelperAttributeValue(MarkupTagHelperAttributeValueSyntax node)
    {
        // We don't generate a classified span when the attribute value is a simple literal value.
        // This is done so we maintain the classified spans generated in 2.x which
        // used ConditionalAttributeCollapser (combines markup literal attribute values into one span with no block parent).
        if (node.Children.Count > 1 ||
            (node.Children.Count == 1 && node.Children[0] is MarkupDynamicAttributeValueSyntax))
        {
            WriteBlock(node, BlockKindInternal.Markup, base.VisitMarkupTagHelperAttributeValue);
            return;
        }

        base.VisitMarkupTagHelperAttributeValue(node);
    }

    public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Tag, n =>
        {
            var children = GetRewrittenMarkupStartTagChildren(node);
            foreach (var child in children)
            {
                Visit(child);
            }
        });
    }

    public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Tag, n =>
        {
            var children = GetRewrittenMarkupEndTagChildren(node);
            foreach (var child in children)
            {
                Visit(child);
            }
        });
    }

    public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Tag, base.VisitMarkupTagHelperElement);
    }

    public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
    {
        foreach (var child in node.Attributes)
        {
            if (child is MarkupTagHelperAttributeSyntax ||
                child is MarkupTagHelperDirectiveAttributeSyntax ||
                child is MarkupMinimizedTagHelperDirectiveAttributeSyntax)
            {
                Visit(child);
            }
        }
    }

    public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
    {
        // We don't want to generate a classified span for a tag helper end tag. Do nothing.
    }

    public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Markup, n =>
        {
            var equalsSyntax = SyntaxFactory.MarkupTextLiteral(new SyntaxList<SyntaxToken>(node.EqualsToken));
            var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name, node.NameSuffix, equalsSyntax, node.ValuePrefix);
            Visit(mergedAttributePrefix);
            Visit(node.Value);
            Visit(node.ValueSuffix);
        });
    }

    public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
    {
        Visit(node.Value);
    }

    public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
    {
        Visit(node.Transition);
        Visit(node.Colon);
        Visit(node.Value);
    }

    public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
    {
        Visit(node.Transition);
        Visit(node.Colon);
    }

    public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Markup, n =>
        {
            var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name);
            Visit(mergedAttributePrefix);
        });
    }

    public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
    {
        WriteBlock(node, BlockKindInternal.HtmlComment, base.VisitMarkupCommentBlock);
    }

    public override void VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
    {
        WriteBlock(node, BlockKindInternal.Markup, base.VisitMarkupDynamicAttributeValue);
    }

    public override void VisitRazorMetaCode(RazorMetaCodeSyntax node)
    {
        WriteSpan(node, SpanKindInternal.MetaCode);
        base.VisitRazorMetaCode(node);
    }

    public override void VisitCSharpTransition(CSharpTransitionSyntax node)
    {
        WriteSpan(node, SpanKindInternal.Transition);
        base.VisitCSharpTransition(node);
    }

    public override void VisitMarkupTransition(MarkupTransitionSyntax node)
    {
        WriteSpan(node, SpanKindInternal.Transition);
        base.VisitMarkupTransition(node);
    }

    public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
    {
        WriteSpan(node, SpanKindInternal.Code);
        base.VisitCSharpStatementLiteral(node);
    }

    public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
    {
        WriteSpan(node, SpanKindInternal.Code);
        base.VisitCSharpExpressionLiteral(node);
    }

    public override void VisitCSharpEphemeralTextLiteral(CSharpEphemeralTextLiteralSyntax node)
    {
        WriteSpan(node, SpanKindInternal.Code);
        base.VisitCSharpEphemeralTextLiteral(node);
    }

    public override void VisitUnclassifiedTextLiteral(UnclassifiedTextLiteralSyntax node)
    {
        WriteSpan(node, SpanKindInternal.None);
        base.VisitUnclassifiedTextLiteral(node);
    }

    public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
    {
        WriteSpan(node, SpanKindInternal.Markup);
        base.VisitMarkupLiteralAttributeValue(node);
    }

    public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
    {
        if (node.Parent is MarkupLiteralAttributeValueSyntax)
        {
            base.VisitMarkupTextLiteral(node);
            return;
        }

        WriteSpan(node, SpanKindInternal.Markup);
        base.VisitMarkupTextLiteral(node);
    }

    public override void VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
    {
        WriteSpan(node, SpanKindInternal.Markup);
        base.VisitMarkupEphemeralTextLiteral(node);
    }

    private void WriteBlock<TNode>(TNode node, BlockKindInternal kind, Action<TNode> handler) where TNode : SyntaxNode
    {
        var previousBlock = _currentBlock;
        var previousKind = _currentBlockKind;

        _currentBlock = node;
        _currentBlockKind = kind;

        handler(node);

        _currentBlock = previousBlock;
        _currentBlockKind = previousKind;
    }

    private void WriteSpan(SyntaxNode node, SpanKindInternal kind, AcceptedCharactersInternal? acceptedCharacters = null)
    {
        if (node.IsMissing)
        {
            return;
        }

        var spanSource = node.GetSourceSpan(_source);
        var blockSource = _currentBlock.GetSourceSpan(_source);
        if (!acceptedCharacters.HasValue)
        {
            acceptedCharacters = AcceptedCharactersInternal.Any;
            var context = node.GetSpanContext();
            if (context != null)
            {
                acceptedCharacters = context.EditHandler.AcceptedCharacters;
            }
        }

        var span = new ClassifiedSpanInternal(spanSource, blockSource, kind, _currentBlockKind, acceptedCharacters.Value);
        _spans.Add(span);
    }

    private static SyntaxList<RazorSyntaxNode> GetRewrittenMarkupStartTagChildren(MarkupStartTagSyntax node)
    {
        // Rewrites the children of the start tag to look like the legacy syntax tree.
        if (node.IsMarkupTransition)
        {
            var tokens = node.DescendantNodes().Where(n => n is SyntaxToken token && !token.IsMissing).Cast<SyntaxToken>().ToArray();
            var tokenBuilder = SyntaxListBuilder<SyntaxToken>.Create();
            tokenBuilder.AddRange(tokens, 0, tokens.Length);
            var markupTransition = SyntaxFactory.MarkupTransition(tokenBuilder.ToList()).Green.CreateRed(node, node.Position);
            var spanContext = node.GetSpanContext();
            if (spanContext != null)
            {
                markupTransition = markupTransition.WithSpanContext(spanContext);
            }

            var builder = new SyntaxListBuilder(1);
            builder.Add(markupTransition);
            return new SyntaxList<RazorSyntaxNode>(builder.ToListNode().CreateRed(node, node.Position));
        }

        SpanContext latestSpanContext = null;
        var children = node.Children;
        var newChildren = new SyntaxListBuilder(children.Count);
        var literals = new List<MarkupTextLiteralSyntax>();
        foreach (var child in children)
        {
            if (child is MarkupTextLiteralSyntax literal)
            {
                literals.Add(literal);
                latestSpanContext = literal.GetSpanContext() ?? latestSpanContext;
            }
            else if (child is MarkupMiscAttributeContentSyntax miscContent)
            {
                foreach (var contentChild in miscContent.Children)
                {
                    if (contentChild is MarkupTextLiteralSyntax contentLiteral)
                    {
                        literals.Add(contentLiteral);
                        latestSpanContext = contentLiteral.GetSpanContext() ?? latestSpanContext;
                    }
                    else
                    {
                        // Pop stack
                        AddLiteralIfExists();
                        newChildren.Add(contentChild);
                    }
                }
            }
            else
            {
                AddLiteralIfExists();
                newChildren.Add(child);
            }
        }

        AddLiteralIfExists();

        return new SyntaxList<RazorSyntaxNode>(newChildren.ToListNode().CreateRed(node, node.Position));

        void AddLiteralIfExists()
        {
            if (literals.Count > 0)
            {
                var mergedLiteral = SyntaxUtilities.MergeTextLiterals(literals.ToArray());
                mergedLiteral = mergedLiteral.WithSpanContext(latestSpanContext);
                literals.Clear();
                latestSpanContext = null;
                newChildren.Add(mergedLiteral);
            }
        }
    }

    private static SyntaxList<RazorSyntaxNode> GetRewrittenMarkupEndTagChildren(MarkupEndTagSyntax node)
    {
        // Rewrites the children of the end tag to look like the legacy syntax tree.
        if (node.IsMarkupTransition)
        {
            var tokens = node.DescendantNodes().Where(n => n is SyntaxToken token && !token.IsMissing).Cast<SyntaxToken>().ToArray();
            var tokenBuilder = SyntaxListBuilder<SyntaxToken>.Create();
            tokenBuilder.AddRange(tokens, 0, tokens.Length);
            var markupTransition = SyntaxFactory.MarkupTransition(tokenBuilder.ToList()).Green.CreateRed(node, node.Position);
            var spanContext = node.GetSpanContext();
            if (spanContext != null)
            {
                markupTransition = markupTransition.WithSpanContext(spanContext);
            }

            var builder = new SyntaxListBuilder(1);
            builder.Add(markupTransition);
            return new SyntaxList<RazorSyntaxNode>(builder.ToListNode().CreateRed(node, node.Position));
        }

        return node.Children;
    }
}
