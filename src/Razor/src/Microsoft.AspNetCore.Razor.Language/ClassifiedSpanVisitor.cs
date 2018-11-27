// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class ClassifiedSpanVisitor : SyntaxWalker
    {
        private RazorSourceDocument _source;
        private List<ClassifiedSpanInternal> _spans;
        private BlockKindInternal _currentBlockKind;
        private SyntaxNode _currentBlock;

        public ClassifiedSpanVisitor(RazorSourceDocument source)
        {
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

        public override void VisitMarkupTagBlock(MarkupTagBlockSyntax node)
        {
            WriteBlock(node, BlockKindInternal.Tag, base.VisitMarkupTagBlock);
        }

        public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
        {
            WriteBlock(node, BlockKindInternal.Tag, base.VisitMarkupTagHelperElement);
        }

        public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
        {
            foreach (var child in node.Children)
            {
                if (child is MarkupTagHelperAttributeSyntax attribute)
                {
                    Visit(attribute);
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
                var mergedAttributePrefix = MergeTextLiteralSpans(node.NamePrefix, node.Name, node.NameSuffix, equalsSyntax, node.ValuePrefix);
                Visit(mergedAttributePrefix);
                Visit(node.Value);
                Visit(node.ValueSuffix);
            });
        }

        public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
        {
            Visit(node.Value);
        }

        public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
        {
            WriteBlock(node, BlockKindInternal.Markup, n =>
            {
                var mergedAttributePrefix = MergeTextLiteralSpans(node.NamePrefix, node.Name);
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

        private MarkupTextLiteralSyntax MergeTextLiteralSpans(params MarkupTextLiteralSyntax[] literalSyntaxes)
        {
            if (literalSyntaxes == null || literalSyntaxes.Length == 0)
            {
                return null;
            }

            SyntaxNode parent = null;
            var position = 0;
            var seenFirstLiteral = false;
            var builder = Syntax.InternalSyntax.SyntaxListBuilder.Create();

            foreach (var syntax in literalSyntaxes)
            {
                if (syntax == null)
                {
                    continue;
                }
                else if (!seenFirstLiteral)
                {
                    // Set the parent and position of the merged literal to the value of the first non-null literal.
                    parent = syntax.Parent;
                    position = syntax.Position;
                    seenFirstLiteral = true;
                }

                foreach (var token in syntax.LiteralTokens)
                {
                    builder.Add(token.Green);
                }
            }

            var mergedLiteralSyntax = Syntax.InternalSyntax.SyntaxFactory.MarkupTextLiteral(
                builder.ToList<Syntax.InternalSyntax.SyntaxToken>());

            return (MarkupTextLiteralSyntax)mergedLiteralSyntax.CreateRed(parent, position);
        }
    }
}
