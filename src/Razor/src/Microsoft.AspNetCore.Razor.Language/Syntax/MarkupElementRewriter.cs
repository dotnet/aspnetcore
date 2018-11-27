// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal static class MarkupElementRewriter
    {
        public static RazorSyntaxTree AddMarkupElements(RazorSyntaxTree syntaxTree)
        {
            var rewriter = new AddMarkupElementRewriter();
            var rewrittenRoot = rewriter.Visit(syntaxTree.Root);

            var newSyntaxTree = RazorSyntaxTree.Create(rewrittenRoot, syntaxTree.Source, syntaxTree.Diagnostics, syntaxTree.Options);
            return newSyntaxTree;
        }

        public static RazorSyntaxTree RemoveMarkupElements(RazorSyntaxTree syntaxTree)
        {
            var rewriter = new RemoveMarkupElementRewriter();
            var rewrittenRoot = rewriter.Visit(syntaxTree.Root);

            var newSyntaxTree = RazorSyntaxTree.Create(rewrittenRoot, syntaxTree.Source, syntaxTree.Diagnostics, syntaxTree.Options);
            return newSyntaxTree;
        }

        private class AddMarkupElementRewriter : SyntaxRewriter
        {
            private readonly Stack<TagBlockTracker> _startTagTracker = new Stack<TagBlockTracker>();

            private TagBlockTracker CurrentTracker => _startTagTracker.Count > 0 ? _startTagTracker.Peek() : null;

            private string CurrentStartTagName => CurrentTracker?.TagName;

            public override SyntaxNode Visit(SyntaxNode node)
            {
                node = base.Visit(node);
                
                if (node != null)
                {
                    node = RewriteNode(node);
                }

                return node;
            }

            private SyntaxNode RewriteNode(SyntaxNode node)
            {
                if (node.IsToken)
                {
                    // Tokens don't have children.
                    return node;
                }

                _startTagTracker.Clear();
                var children = node.ChildNodes().ToList();
                var rewrittenChildren = new List<SyntaxNode>(children.Count);
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!(child is MarkupTagBlockSyntax tagBlock))
                    {
                        TrackChild(child, rewrittenChildren);
                        continue;
                    }

                    var tagName = tagBlock.GetTagName();
                    if (string.IsNullOrWhiteSpace(tagName) || tagBlock.IsSelfClosing())
                    {
                        // Don't want to track incomplete, invalid (Eg. </>, <  >), void or self-closing tags.
                        // Simply wrap it in a block with no body or start/end tag.
                        if (IsEndTag(tagBlock))
                        {
                            // This is an error case.
                            BuildMarkupElement(rewrittenChildren, startTag: null, tagChildren: new List<RazorSyntaxNode>(), endTag: tagBlock);
                        }
                        else
                        {
                            BuildMarkupElement(rewrittenChildren, startTag: tagBlock, tagChildren: new List<RazorSyntaxNode>(), endTag: null);
                        }
                    }
                    else if (IsEndTag(tagBlock))
                    {
                        if (string.Equals(CurrentStartTagName, tagName, StringComparison.OrdinalIgnoreCase))
                        {
                            var startTagTracker = _startTagTracker.Pop();
                            var startTag = startTagTracker.TagBlock;

                            // Get the nodes between the start and the end tag.
                            var tagChildren = startTagTracker.Children;

                            BuildMarkupElement(rewrittenChildren, startTag, tagChildren, endTag: tagBlock);
                        }
                        else
                        {
                            // Current tag scope does not match the end tag. Attempt to recover the start tag
                            // by looking up the previous tag scopes for a matching start tag.
                            if (!TryRecoverStartTag(rewrittenChildren, tagName, tagBlock))
                            {
                                // Could not recover. The end tag doesn't have a corresponding start tag. Wrap it in a block and move on.
                                var rewritten = SyntaxFactory.MarkupElement(startTag: null, body: new SyntaxList<RazorSyntaxNode>(), endTag: tagBlock);
                                TrackChild(rewritten, rewrittenChildren);
                            }
                        }
                    }
                    else
                    {
                        // This is a start tag. Keep track of it.
                        _startTagTracker.Push(new TagBlockTracker(tagBlock));
                    }
                }

                while (_startTagTracker.Count > 0)
                {
                    // We reached the end of the list and still have unmatched start tags
                    var startTagTracker = _startTagTracker.Pop();
                    var startTag = startTagTracker.TagBlock;
                    var tagChildren = startTagTracker.Children;
                    BuildMarkupElement(rewrittenChildren, startTag, tagChildren, endTag: null);
                }

                // We now have finished building our list of rewritten Children.
                // At this point, We should have a one to one replacement for every child. The replacement can be null.
                Debug.Assert(children.Count == rewrittenChildren.Count);
                node = node.ReplaceNodes(children, (original, rewritten) =>
                {
                    var originalIndex = children.IndexOf(original);
                    if (originalIndex != -1)
                    {
                        // If this returns null, that node will be removed.
                        return rewrittenChildren[originalIndex];
                    }

                    return original;
                });

                return node;
            }

            private void BuildMarkupElement(List<SyntaxNode> rewrittenChildren, MarkupTagBlockSyntax startTag, List<RazorSyntaxNode> tagChildren, MarkupTagBlockSyntax endTag)
            {
                // We are trying to replace multiple nodes (including the start/end tag) with one rewritten node.
                // Since we need to have each child node accounted for in our rewritten list,
                // we'll add "null" in place of them.
                // The call to SyntaxNode.ReplaceNodes() later will take care removing the nodes whose replacement is null.

                var body = tagChildren.Where(t => t != null).ToList();
                var rewritten = SyntaxFactory.MarkupElement(startTag, new SyntaxList<RazorSyntaxNode>(body), endTag);
                if (startTag != null)
                {
                    // If there was a start tag, that is where we want to put our new element.
                    TrackChild(rewritten, rewrittenChildren);
                }

                foreach (var child in tagChildren)
                {
                    TrackChild(null, rewrittenChildren);
                }
                if (endTag != null)
                {
                    TrackChild(startTag == null ? rewritten : null, rewrittenChildren);
                }
            }

            private void TrackChild(SyntaxNode child, List<SyntaxNode> rewrittenChildren)
            {
                if (CurrentTracker != null)
                {
                    CurrentTracker.Children.Add((RazorSyntaxNode)child);
                    return;
                }

                rewrittenChildren.Add(child);
            }

            private bool TryRecoverStartTag(List<SyntaxNode> rewrittenChildren, string tagName, MarkupTagBlockSyntax endTag)
            {
                var malformedTagCount = 0;
                foreach (var tracker in _startTagTracker)
                {
                    if (tracker.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    malformedTagCount++;
                }

                if (_startTagTracker.Count > malformedTagCount)
                {
                    RewriteMalformedTags(rewrittenChildren, malformedTagCount);

                    // One final rewrite, this is the rewrite that completes our target tag which is not malformed.
                    var startTagTracker = _startTagTracker.Pop();
                    var startTag = startTagTracker.TagBlock;
                    var tagChildren = startTagTracker.Children;

                    BuildMarkupElement(rewrittenChildren, startTag, tagChildren, endTag);

                    // We were able to recover
                    return true;
                }

                // Could not recover tag. Aka we found an end tag without a corresponding start tag.
                return false;
            }

            private void RewriteMalformedTags(List<SyntaxNode> rewrittenChildren, int malformedTagCount)
            {
                for (var i = 0; i < malformedTagCount; i++)
                {
                    var startTagTracker = _startTagTracker.Pop();
                    var startTag = startTagTracker.TagBlock;

                    BuildMarkupElement(rewrittenChildren, startTag, startTagTracker.Children, endTag: null);
                }
            }

            private bool IsEndTag(MarkupTagBlockSyntax tagBlock)
            {
                var childContent = tagBlock.Children.First().GetContent();
                if (string.IsNullOrEmpty(childContent))
                {
                    return false;
                }

                // We grab the token that could be forward slash
                return childContent.StartsWith("</") || childContent.StartsWith("/");
            }

            private class TagBlockTracker
            {
                public TagBlockTracker(MarkupTagBlockSyntax tagBlock)
                {
                    TagBlock = tagBlock;
                    TagName = tagBlock.GetTagName();
                    Children = new List<RazorSyntaxNode>();
                }

                public MarkupTagBlockSyntax TagBlock { get; }

                public List<RazorSyntaxNode> Children { get; }

                public string TagName { get; }
            }
        }

        private class RemoveMarkupElementRewriter : SyntaxRewriter
        {
            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node != null)
                {
                    node = RewriteNode(node);
                }

                return base.Visit(node);
            }

            private SyntaxNode RewriteNode(SyntaxNode node)
            {
                if (node.IsToken)
                {
                    return node;
                }

                var children = node.ChildNodes();
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!(child is MarkupElementSyntax tagElement))
                    {
                        continue;
                    }

                    node = node.ReplaceNode(tagElement, tagElement.ChildNodes());

                    // Since we rewrote 'node', it's children are different. Update our collection.
                    children = node.ChildNodes();
                }

                return node;
            }
        }
    }
}