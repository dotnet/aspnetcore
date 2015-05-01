// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser.TagHelpers.Internal
{
    public class TagHelperParseTreeRewriter : ISyntaxTreeRewriter
    {
        private TagHelperDescriptorProvider _provider;
        private Stack<TagHelperBlockTracker> _trackerStack;
        private Stack<BlockBuilder> _blockStack;
        private BlockBuilder _currentBlock;

        public TagHelperParseTreeRewriter(TagHelperDescriptorProvider provider)
        {
            _provider = provider;
            _trackerStack = new Stack<TagHelperBlockTracker>();
            _blockStack = new Stack<BlockBuilder>();
        }

        public void Rewrite(RewritingContext context)
        {
            RewriteTags(context.SyntaxTree, context);

            context.SyntaxTree = _currentBlock.Build();
        }

        private void RewriteTags(Block input, RewritingContext context)
        {
            // We want to start a new block without the children from existing (we rebuild them).
            TrackBlock(new BlockBuilder
            {
                Type = input.Type,
                CodeGenerator = input.CodeGenerator
            });

            var activeTagHelpers = _trackerStack.Count;

            foreach (var child in input.Children)
            {
                if (child.IsBlock)
                {
                    var childBlock = (Block)child;

                    if (childBlock.Type == BlockType.Tag)
                    {
                        if (TryRewriteTagHelper(childBlock, context))
                        {
                            continue;
                        }

                        // If we get to here it means that we're a normal html tag.  No need to iterate any deeper into
                        // the children of it because they wont be tag helpers.
                    }
                    else
                    {
                        // We're not an Html tag so iterate through children recursively.
                        RewriteTags(childBlock, context);
                        continue;
                    }
                }

                // At this point the child is a Span or Block with Type BlockType.Tag that doesn't happen to be a
                // tag helper.

                // Add the child to current block. 
                _currentBlock.Children.Add(child);
            }

            // We captured the number of active tag helpers at the start of our logic, it should be the same. If not
            // it means that there are malformed tag helpers at the top of our stack.
            if (activeTagHelpers != _trackerStack.Count)
            {
                // Malformed tag helpers built here will be tag helpers that do not have end tags in the current block 
                // scope. Block scopes are special cases in Razor such as @<p> would cause an error because there's no
                // matching end </p> tag in the template block scope and therefore doesn't make sense as a tag helper.
                BuildMalformedTagHelpers(_trackerStack.Count - activeTagHelpers, context);

                Debug.Assert(activeTagHelpers == _trackerStack.Count);
            }

            BuildCurrentlyTrackedBlock();
        }

        private bool TryRewriteTagHelper(Block tagBlock, RewritingContext context)
        {
            // Get tag name of the current block (doesn't matter if it's an end or start tag)
            var tagName = GetTagName(tagBlock);

            // Could not determine tag name, it can't be a TagHelper, continue on and track the element.
            if (tagName == null)
            {
                return false;
            }

            var descriptors = Enumerable.Empty<TagHelperDescriptor>();

            if (!IsPotentialTagHelper(tagName, tagBlock))
            {
                return false;
            }

            var tracker = _trackerStack.Count > 0 ? _trackerStack.Peek() : null;
            var tagNameScope = tracker?.Builder.TagName ?? string.Empty;

            if (!IsEndTag(tagBlock))
            {
                // We're now in a start tag block, we first need to see if the tag block is a tag helper.
                var providedAttributes = GetAttributeNames(tagBlock);

                descriptors = _provider.GetDescriptors(tagName, providedAttributes);

                // If there aren't any TagHelperDescriptors registered then we aren't a TagHelper
                if (!descriptors.Any())
                {
                    // If the current tag matches the current TagHelper scope it means the parent TagHelper matched
                    // all the required attributes but the current one did not; therefore, we need to increment the 
                    // OpenMatchingTags counter for current the TagHelperBlock so we don't end it too early.
                    // ex: <myth req="..."><myth></myth></myth> We don't want the first myth to close on the inside
                    // tag.
                    if (string.Equals(tagNameScope, tagName, StringComparison.OrdinalIgnoreCase))
                    {
                        tracker.OpenMatchingTags++;
                    }

                    return false;
                }

                // We're in a start TagHelper block.
                var validTagStructure = ValidateTagStructure(tagName, tagBlock, context);

                var builder = TagHelperBlockRewriter.Rewrite(
                    tagName,
                    validTagStructure,
                    tagBlock,
                    descriptors,
                    context.ErrorSink);

                // Track the original start tag so the editor knows where each piece of the TagHelperBlock lies 
                // for formatting.
                builder.SourceStartTag = tagBlock;

                // Found a new tag helper block
                TrackTagHelperBlock(builder);

                // If it's a self closing block then we don't have to worry about nested children 
                // within the tag... complete it.
                if (builder.SelfClosing)
                {
                    BuildCurrentlyTrackedTagHelperBlock(endTag: null);
                }
            }
            else
            {
                // Validate that our end tag matches the currently scoped tag, if not we may need to error.
                if (tagNameScope.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                {
                    // If there are additional end tags required before we can build our block it means we're in a
                    // situation like this: <myth req="..."><myth></myth></myth> where we're at the inside </myth>.
                    if (tracker.OpenMatchingTags > 0)
                    {
                        tracker.OpenMatchingTags--;

                        return false;
                    }

                    ValidateTagStructure(tagName, tagBlock, context);

                    BuildCurrentlyTrackedTagHelperBlock(tagBlock);
                }
                else
                {
                    // If there are not TagHelperDescriptors associated with the end tag block that also have no 
                    // required attributes then it means we can't be a TagHelper, bail out.
                    if (!_provider.GetDescriptors(tagName, attributeNames: Enumerable.Empty<string>()).Any())
                    {
                        return false;
                    }

                    // Current tag helper scope does not match the end tag. Attempt to recover the tag 
                    // helper by looking up the previous tag helper scopes for a matching tag. If we 
                    // can't recover it means there was no corresponding tag helper start tag.
                    if (TryRecoverTagHelper(tagName, tagBlock, context))
                    {
                        ValidateTagStructure(tagName, tagBlock, context);

                        // Successfully recovered, move onto the next element.
                    }
                    else
                    {
                        // Could not recover, the end tag helper has no corresponding start tag, create
                        // an error based on the current childBlock.
                        context.ErrorSink.OnError(
                            tagBlock.Start,
                            RazorResources.FormatTagHelpersParseTreeRewriter_FoundMalformedTagHelper(tagName));

                        return false;
                    }
                }
            }

            return true;
        }

        private IEnumerable<string> GetAttributeNames(Block tagBlock)
        {
            // Need to calculate how many children we should take that represent the attributes.
            var childrenOffset = IsPartialTag(tagBlock) ? 1 : 2;
            var attributeChildren = tagBlock.Children.Skip(1).Take(tagBlock.Children.Count() - childrenOffset);
            var attributeNames = new List<string>();

            foreach (var child in attributeChildren)
            {
                Span childSpan;

                if (child.IsBlock)
                {
                    childSpan = ((Block)child).FindFirstDescendentSpan();

                    if (childSpan == null)
                    {
                        continue;
                    }
                }
                else
                {
                    childSpan = child as Span;
                }

                var attributeName = childSpan
                    .Content
                    .Split(separator: new[] { '=' }, count: 2)[0]
                    .TrimStart();

                attributeNames.Add(attributeName);
            }

            return attributeNames;
        }

        private static bool ValidateTagStructure(string tagName, Block tag, RewritingContext context)
        {
            // We assume an invalid structure until we verify that the tag meets all of our "valid structure" criteria.
            if (IsPartialTag(tag))
            {
                context.ErrorSink.OnError(
                    tag.Start,
                    RazorResources.FormatTagHelpersParseTreeRewriter_MissingCloseAngle(tagName));

                return false;
            }

            return true;
        }

        private static bool IsPartialTag(Block tagBlock)
        {
            // No need to validate the tag end because in order to be a tag block it must start with '<'.
            var tagEnd = tagBlock.Children.Last() as Span;

            // If our tag end is not a markup span it means it's some sort of code SyntaxTreeNode (not a valid format)
            if (tagEnd != null && tagEnd.Kind == SpanKind.Markup)
            {
                var endSymbol = tagEnd.Symbols.LastOrDefault() as HtmlSymbol;

                if (endSymbol != null && endSymbol.Type == HtmlSymbolType.CloseAngle)
                {
                    return false;
                }
            }

            return true;
        }

        private void BuildCurrentlyTrackedBlock()
        {
            // Going to remove the current BlockBuilder from the stack because it's complete.
            var currentBlock = _blockStack.Pop();

            // If there are block stacks left it means we're not at the root.
            if (_blockStack.Count > 0)
            {
                // Grab the next block in line so we can continue managing its children (it's not done).
                var previousBlock = _blockStack.Peek();

                // We've finished the currentBlock so build it and add it to its parent.
                previousBlock.Children.Add(currentBlock.Build());

                // Update the _currentBlock to point at the last tracked block because it's not complete.
                _currentBlock = previousBlock;
            }
            else
            {
                _currentBlock = currentBlock;
            }
        }

        private void BuildCurrentlyTrackedTagHelperBlock(Block endTag)
        {
            // Track the original end tag so the editor knows where each piece of the TagHelperBlock lies 
            // for formatting.
            _trackerStack.Pop().Builder.SourceEndTag = endTag;

            BuildCurrentlyTrackedBlock();
        }

        private bool IsPotentialTagHelper(string tagName, Block childBlock)
        {
            var child = childBlock.Children.FirstOrDefault();
            Debug.Assert(child != null);

            var childSpan = (Span)child;

            // text tags that are labeled as transitions should be ignored aka they're not tag helpers.
            return !string.Equals(tagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase) ||
                   childSpan.Kind != SpanKind.Transition;
        }

        private void TrackBlock(BlockBuilder builder)
        {
            _currentBlock = builder;

            _blockStack.Push(builder);
        }

        private void TrackTagHelperBlock(TagHelperBlockBuilder builder)
        {
            _trackerStack.Push(new TagHelperBlockTracker(builder));

            TrackBlock(builder);
        }

        private bool TryRecoverTagHelper(string tagName, Block endTag, RewritingContext context)
        {
            var malformedTagHelperCount = 0;

            foreach (var tracker in _trackerStack)
            {
                if (tracker.Builder.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                malformedTagHelperCount++;
            }

            // If the malformedTagHelperCount == _tagStack.Count it means we couldn't find a start tag for the tag 
            // helper, can't recover.
            if (malformedTagHelperCount != _trackerStack.Count)
            {
                BuildMalformedTagHelpers(malformedTagHelperCount, context);

                // One final build, this is the build that completes our target tag helper block which is not malformed.
                BuildCurrentlyTrackedTagHelperBlock(endTag);

                // We were able to recover
                return true;
            }

            // Could not recover tag helper. Aka we found a tag helper end tag without a corresponding start tag.
            return false;
        }

        private void BuildMalformedTagHelpers(int count, RewritingContext context)
        {
            for (var i = 0; i < count; i++)
            {
                var malformedTagHelper = _trackerStack.Peek().Builder;

                context.ErrorSink.OnError(
                    malformedTagHelper.Start,
                    RazorResources.FormatTagHelpersParseTreeRewriter_FoundMalformedTagHelper(
                        malformedTagHelper.TagName));

                BuildCurrentlyTrackedTagHelperBlock(endTag: null);
            }
        }

        private static string GetTagName(Block tagBlock)
        {
            var child = tagBlock.Children.First();

            if (tagBlock.Type != BlockType.Tag || !tagBlock.Children.Any() || !(child is Span))
            {
                return null;
            }

            var childSpan = (Span)child;
            var textSymbol = childSpan.Symbols.FirstHtmlSymbolAs(HtmlSymbolType.WhiteSpace | HtmlSymbolType.Text);

            if (textSymbol == null)
            {
                return null;
            }

            return textSymbol.Type == HtmlSymbolType.WhiteSpace ? null : textSymbol.Content;
        }

        private static bool IsEndTag(Block tagBlock)
        {
            EnsureTagBlock(tagBlock);

            var childSpan = (Span)tagBlock.Children.First();
            // We grab the symbol that could be forward slash
            var relevantSymbol = (HtmlSymbol)childSpan.Symbols.Take(2).Last();

            return relevantSymbol.Type == HtmlSymbolType.ForwardSlash;
        }

        private static void EnsureTagBlock(Block tagBlock)
        {
            Debug.Assert(tagBlock.Type == BlockType.Tag);
            Debug.Assert(tagBlock.Children.First() is Span);
        }

        private class TagHelperBlockTracker
        {
            public TagHelperBlockTracker(TagHelperBlockBuilder builder)
            {
                Builder = builder;
            }

            public TagHelperBlockBuilder Builder { get; }

            public uint OpenMatchingTags { get; set; }
        }
    }
}