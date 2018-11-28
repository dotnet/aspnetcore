// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AngleSharp.Html;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    // Rewrites contiguous subtrees of HTML into a special node type to reduce the
    // size of the Render tree.
    //
    // Does not preserve insignificant details of the HTML, like tag closing style
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

                // We want to fold together siblings where possible. To do this, first we find
                // the index of the node we're looking at now - then we need to walk backwards
                // and identify a set of contiguous nodes we can merge.
                var start = reference.Parent.Children.Count - 1;
                for (; start >= 0; start--)
                {
                    if (ReferenceEquals(reference.Node, reference.Parent.Children[start]))
                    {
                        break;
                    }
                }

                // This is the current node. Check if the left sibling is always a candidate
                // for rewriting. Due to the order we processed the nodes, we know that the
                // left sibling is next in the list to process if it's a candidate.
                var end = start;
                while (start - 1 >= 0)
                {
                    var candidate = reference.Parent.Children[start - 1];
                    if (trees.Count == 0 || !ReferenceEquals(trees[trees.Count - 1].Node, candidate))
                    {
                        // This means the we're out of nodes, or the left sibling is not in the list.
                        break;
                    }

                    // This means that the left sibling is valid to merge.
                    start--;

                    // Remove this since we're combining it.
                    trees.RemoveAt(trees.Count - 1);
                }

                // As a degenerate case, don't bother rewriting an single HtmlContent node
                // It doesn't add any value.
                if (end - start == 0 && reference.Node is HtmlContentIntermediateNode)
                {
                    continue;
                }

                // Now we know the range of nodes to rewrite (end is inclusive)
                var length = end + 1 - start;
                while (length > 0)
                {
                    // Keep using start since we're removing nodes.
                    var node = reference.Parent.Children[start];
                    reference.Parent.Children.RemoveAt(start);

                    rewriteVisitor.Visit(node);

                    length--;
                }

                reference.Parent.Children.Insert(start, new HtmlBlockIntermediateNode()
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
                // We need to restore the state after processing this node.
                // We might have found a leaf-block of HTML, but that shouldn't
                // affect our parent's state.
                var originalState = _foundNonHtml;

                _foundNonHtml = false;

                if (node.HasDiagnostics)
                {
                    // Treat node with errors as non-HTML
                    _foundNonHtml = true;
                }
                
                // Visit Children
                base.VisitDefault(node);

                if (!_foundNonHtml)
                {
                    Trees.Add(new IntermediateNodeReference(Parent, node));
                }

                _foundNonHtml = originalState |= _foundNonHtml;
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
            private readonly StringBuilder _encodingBuilder;

            private readonly List<IntermediateNodeReference> _trees;

            public RewriteVisitor(List<IntermediateNodeReference> trees)
            {
                _trees = trees;

                _encodingBuilder = new StringBuilder();
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
                // start/end tag.
                if (!hasBodyContent && isVoid)
                {
                    // void
                    Builder.Append(">");
                    return;
                }
                else if (!hasBodyContent)
                {
                    // In HTML5, we can't have self-closing non-void elements, so explicitly
                    // add a close tag
                    Builder.Append("></");
                    Builder.Append(node.TagName);
                    Builder.Append(">");
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
                Builder.Append(Encode(node.Children));
            }

            public override void VisitHtml(HtmlContentIntermediateNode node)
            {
                Builder.Append(Encode(node.Children));
            }

            private string Encode(IntermediateNodeCollection nodes)
            {
                // We need to HTML encode text content. We would have decoded HTML entities
                // earlier when we parsed the text into a tree, but since we're folding
                // this node into a block of pre-encoded HTML we need to be sure to
                // re-encode.
                _encodingBuilder.Clear();

                for (var i = 0; i < nodes.Count; i++)
                {
                    _encodingBuilder.Append(((IntermediateToken)nodes[i]).Content);
                }

                return HtmlMarkupFormatter.Instance.Text(_encodingBuilder.ToString());
            }
        }
    }
}
