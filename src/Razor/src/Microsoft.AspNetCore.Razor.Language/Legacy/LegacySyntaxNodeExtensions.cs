// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal static class LegacySyntaxNodeExtensions
    {
        private static readonly SyntaxKind[] TransitionSpanKinds = new SyntaxKind[]
        {
            SyntaxKind.CSharpTransition,
            SyntaxKind.MarkupTransition,
        };

        private static readonly SyntaxKind[] MetaCodeSpanKinds = new SyntaxKind[]
        {
            SyntaxKind.RazorMetaCode,
        };

        private static readonly SyntaxKind[] CommentSpanKinds = new SyntaxKind[]
        {
            SyntaxKind.RazorCommentLiteral,
        };

        private static readonly SyntaxKind[] CodeSpanKinds = new SyntaxKind[]
        {
            SyntaxKind.CSharpStatementLiteral,
            SyntaxKind.CSharpExpressionLiteral,
            SyntaxKind.CSharpEphemeralTextLiteral,
        };

        private static readonly SyntaxKind[] MarkupSpanKinds = new SyntaxKind[]
        {
            SyntaxKind.MarkupTextLiteral,
            SyntaxKind.MarkupEphemeralTextLiteral,
        };

        private static readonly SyntaxKind[] NoneSpanKinds = new SyntaxKind[]
        {
            SyntaxKind.UnclassifiedTextLiteral,
        };

        public static SpanContext GetSpanContext(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var context = node.GetAnnotationValue(SyntaxConstants.SpanContextKind);

            return context is SpanContext ? (SpanContext)context : null;
        }

        public static TNode WithSpanContext<TNode>(this TNode node, SpanContext spanContext) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var newAnnotation = new SyntaxAnnotation(SyntaxConstants.SpanContextKind, spanContext);

            var newAnnotations = new List<SyntaxAnnotation>();
            newAnnotations.Add(newAnnotation);
            foreach (var annotation in node.GetAnnotations())
            {
                if (annotation.Kind != newAnnotation.Kind)
                {
                    newAnnotations.Add(annotation);
                }
            }

            return node.WithAnnotations(newAnnotations.ToArray());
        }

        public static SyntaxNode LocateOwner(this SyntaxNode node, SourceChange change)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (change.Span.AbsoluteIndex < node.Position)
            {
                // Early escape for cases where changes overlap multiple spans
                // In those cases, the span will return false, and we don't want to search the whole tree
                // So if the current span starts after the change, we know we've searched as far as we need to
                return null;
            }

            if (IsSpanKind(node))
            {
                var editHandler = node.GetSpanContext()?.EditHandler ?? SpanEditHandler.CreateDefault();
                return editHandler.OwnsChange(node, change) ? node : null;
            }

            SyntaxNode owner = null;
            var children = node.ChildNodes();
            foreach (var child in children)
            {
                owner = LocateOwner(child, change);
                if (owner != null)
                {
                    break;
                }
            }

            return owner;
        }

        public static bool IsTransitionSpanKind(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return TransitionSpanKinds.Contains(node.Kind);
        }

        public static bool IsMetaCodeSpanKind(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return MetaCodeSpanKinds.Contains(node.Kind);
        }

        public static bool IsCommentSpanKind(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return CommentSpanKinds.Contains(node.Kind);
        }

        public static bool IsCodeSpanKind(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return CodeSpanKinds.Contains(node.Kind);
        }

        public static bool IsMarkupSpanKind(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return MarkupSpanKinds.Contains(node.Kind);
        }

        public static bool IsNoneSpanKind(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return NoneSpanKinds.Contains(node.Kind);
        }

        public static bool IsSpanKind(this SyntaxNode node)
        {
            return IsTransitionSpanKind(node) ||
                IsMetaCodeSpanKind(node) ||
                IsCommentSpanKind(node) ||
                IsCodeSpanKind(node) ||
                IsMarkupSpanKind(node) ||
                IsNoneSpanKind(node);
        }

        public static IEnumerable<SyntaxNode> FlattenSpans(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            foreach (var child in node.DescendantNodes())
            {
                if (child.IsSpanKind())
                {
                    yield return child;
                }
            }
        }

        public static SyntaxNode PreviousSpan(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var parent = node.Parent;
            while (parent != null)
            {
                var flattenedSpans = parent.FlattenSpans();
                var prevSpan = flattenedSpans.LastOrDefault(n => n.EndPosition <= node.Position && n != node);
                if (prevSpan != null)
                {
                    return prevSpan;
                }

                parent = parent.Parent;
            }

            return null;
        }

        public static SyntaxNode NextSpan(this SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var parent = node.Parent;
            while (parent != null)
            {
                var flattenedSpans = parent.FlattenSpans();
                var nextSpan = flattenedSpans.FirstOrDefault(n => n.Position >= node.EndPosition && n != node);
                if (nextSpan != null)
                {
                    return nextSpan;
                }

                parent = parent.Parent;
            }

            return null;
        }
    }
}
