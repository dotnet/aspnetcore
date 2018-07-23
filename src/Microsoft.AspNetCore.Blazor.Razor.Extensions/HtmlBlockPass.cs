// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // Rewrites contiguous subtrees of HTML into a special node type to reduce the
    // size of the Render tree.
    //
    // Does not preserve insigificant details of the HTML, like tag closing style
    // or quote style.
    internal class HtmlBlockPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Runs LATE because we want to destroy structure.
        public override int Order => 10000;

        protected override void ExecuteCore(
            RazorCodeDocument codeDocument,
            DocumentIntermediateNode documentNode)
        {
            if (documentNode.Options.DesignTime)
            {
                // Nothing to do during design time.
                return;
            }

            var findVisitor = new FindHtmlTreeVisitor();
            findVisitor.Visit(documentNode);

            var trees = findVisitor.Trees;
            var rewriteVisitor = new RewriteVisitor(trees);
            while (trees.Count > 0)
            {
                // Walk backwards since we did a postorder traversal.
                var reference = trees[trees.Count - 1];

                // Forcibly remove a node to prevent infinite loops.
                trees.RemoveAt(trees.Count - 1);

                rewriteVisitor.Visit(reference.Node);
                reference.Replace(new HtmlBlockIntermediateNode()
                {
                    Content = rewriteVisitor.Builder.ToString(),
                });

                rewriteVisitor.Builder.Clear();
            }
        }

        // Finds HTML-blocks using a postorder traversal. We store nodes in an
        // ordered list so we can avoid redundant rewrites.
        //
        // Consider a case like:
        //  <div>
        //    <a href="...">click me</a>
        //  </div>
        //
        // We would store both the div and a tag in a list, but make sure to visit
        // the div first. Then when we process the div (recursively), we would remove
        // the a from the list.
        private class FindHtmlTreeVisitor :
            IntermediateNodeWalker,
            IExtensionIntermediateNodeVisitor<HtmlElementIntermediateNode>
        {
            private bool _foundNonHtml;

            public List<IntermediateNodeReference> Trees { get; } = new List<IntermediateNodeReference>();

            public override void VisitDefault(IntermediateNode node)
            {
                // If we get here, we found a non-HTML node. Keep traversing.
                _foundNonHtml = true;
                base.VisitDefault(node);
            }

            public void VisitExtension(HtmlElementIntermediateNode node)
            {
                // We need to restore the state after processing this node.
                // We might have found a leaf-block of HTML, but that shouldn't
                // affect our parent's state.
                var originalState = _foundNonHtml;

                _foundNonHtml = false;

                if (node.HasDiagnostics)
                {
                    // Treat node with errors as non-HTML - don't let the parent rewrite this either.
                    _foundNonHtml = true;
                }

                if (string.Equals("script", node.TagName, StringComparison.OrdinalIgnoreCase))
                {
                    // Treat script tags as non-HTML - we trigger errors for script tags
                    // later.
                    _foundNonHtml = true;
                }

                base.VisitDefault(node);

                if (!_foundNonHtml)
                {
                    Trees.Add(new IntermediateNodeReference(Parent, node));
                }

                _foundNonHtml = originalState |= _foundNonHtml;
            }

            public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
            {
                if (node.HasDiagnostics)
                {
                    // Treat node with errors as non-HTML
                    _foundNonHtml = true;
                }

                // Visit Children
                base.VisitDefault(node);
            }

            public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
            {
                if (node.HasDiagnostics)
                {
                    // Treat node with errors as non-HTML
                    _foundNonHtml = true;
                }

                // Visit Children
                base.VisitDefault(node);
            }

            public override void VisitHtml(HtmlContentIntermediateNode node)
            {
                if (node.HasDiagnostics)
                {
                    // Treat node with errors as non-HTML
                    _foundNonHtml = true;
                }

                // Visit Children
                base.VisitDefault(node);
            }

            public override void VisitToken(IntermediateToken node)
            {
                if (node.HasDiagnostics)
                {
                    // Treat node with errors as non-HTML
                    _foundNonHtml = true;
                }

                if (node.IsCSharp)
                {
                    _foundNonHtml = true;
                }
            }
        }

        private class RewriteVisitor :
            IntermediateNodeWalker,
            IExtensionIntermediateNodeVisitor<HtmlElementIntermediateNode>
        {
            private readonly List<IntermediateNodeReference> _trees;

            public RewriteVisitor(List<IntermediateNodeReference> trees)
            {
                _trees = trees;
            }

            public StringBuilder Builder { get; } = new StringBuilder();

            public void VisitExtension(HtmlElementIntermediateNode node)
            {
                for (var i = 0; i < _trees.Count; i++)
                {
                    // Remove this node if it's in the list. This ensures that we don't
                    // do redundant operations.
                    if (ReferenceEquals(_trees[i].Node, node))
                    {
                        _trees.RemoveAt(i);
                        break;
                    }
                }

                var isVoid = ComponentDocumentRewritePass.VoidElements.Contains(node.TagName);
                var hasBodyContent = node.Body.Any();

                Builder.Append("<");
                Builder.Append(node.TagName);

                foreach (var attribute in node.Attributes)
                {
                    Visit(attribute);
                }

                // If for some reason a void element contains body, then treat it as a
                // start/end tag. Treat non-void elements without body content as self-closing.
                if (!hasBodyContent && isVoid)
                {
                    // void
                    Builder.Append(">");
                    return;
                }
                else if (!hasBodyContent)
                {
                    // self-closing
                    Builder.Append("/>");
                    return;
                }

                // start/end tag with body.
                Builder.Append(">");

                foreach (var item in node.Body)
                {
                    Visit(item);
                }

                Builder.Append("</");
                Builder.Append(node.TagName);
                Builder.Append(">");
            }

            public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
            {
                Builder.Append(" ");
                Builder.Append(node.AttributeName);

                if (node.Children.Count == 0)
                {
                    // Minimized attribute
                    return;
                }

                Builder.Append("=\"");

                // Visit Children
                base.VisitDefault(node);

                Builder.Append("\"");
            }

            public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
            {
                // Visit Children
                base.VisitDefault(node);
            }

            public override void VisitHtml(HtmlContentIntermediateNode node)
            {
                // Visit Children
                base.VisitDefault(node);
            }

            public override void VisitToken(IntermediateToken node)
            {
                Builder.Append(node.Content);
            }
        }
    }
}
