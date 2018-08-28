// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperParseTreeRewriter
    {
        // Internal for testing.
        // Null characters are invalid markup for HTML attribute values.
        internal static readonly string InvalidAttributeValueMarker = "\0";

        // From http://dev.w3.org/html5/spec/Overview.html#elements-0
        private static readonly HashSet<string> VoidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "area",
            "base",
            "br",
            "col",
            "command",
            "embed",
            "hr",
            "img",
            "input",
            "keygen",
            "link",
            "meta",
            "param",
            "source",
            "track",
            "wbr"
        };

        private readonly string _tagHelperPrefix;
        private readonly List<KeyValuePair<string, string>> _htmlAttributeTracker;
        private readonly StringBuilder _attributeValueBuilder;
        private readonly TagHelperBinder _tagHelperBinder;
        private readonly Stack<TagBlockTracker> _trackerStack;
        private readonly Stack<BlockBuilder> _blockStack;
        private TagHelperBlockTracker _currentTagHelperTracker;
        private BlockBuilder _currentBlock;
        private RazorParserFeatureFlags _featureFlags;

        public TagHelperParseTreeRewriter(
            string tagHelperPrefix,
            IEnumerable<TagHelperDescriptor> descriptors,
            RazorParserFeatureFlags featureFlags)
        {
            _tagHelperPrefix = tagHelperPrefix;
            _tagHelperBinder = new TagHelperBinder(tagHelperPrefix, descriptors);
            _trackerStack = new Stack<TagBlockTracker>();
            _blockStack = new Stack<BlockBuilder>();
            _attributeValueBuilder = new StringBuilder();
            _htmlAttributeTracker = new List<KeyValuePair<string, string>>();
            _featureFlags = featureFlags;
        }

        private TagBlockTracker CurrentTracker => _trackerStack.Count > 0 ? _trackerStack.Peek() : null;

        private string CurrentParentTagName => CurrentTracker?.TagName;

        private bool CurrentParentIsTagHelper => CurrentTracker?.IsTagHelper ?? false;

        public Block Rewrite(Block syntaxTree, ErrorSink errorSink)
        {
            RewriteTags(syntaxTree, errorSink, depth: 0);

            var rewritten = _currentBlock.Build();

            return rewritten;
        }

        private void RewriteTags(Block input, ErrorSink errorSink, int depth)
        {
            // We want to start a new block without the children from existing (we rebuild them).
            TrackBlock(new BlockBuilder
            {
                Type = input.Type,
                ChunkGenerator = input.ChunkGenerator
            });

            var activeTrackers = _trackerStack.Count;

            foreach (var child in input.Children)
            {
                if (child.IsBlock)
                {
                    var childBlock = (Block)child;

                    if (childBlock.Type == BlockKindInternal.Tag)
                    {
                        if (TryRewriteTagHelper(childBlock, errorSink))
                        {
                            continue;
                        }
                        else
                        {
                            // Non-TagHelper tag.
                            ValidateParentAllowsPlainTag(childBlock, errorSink);

                            TrackTagBlock(childBlock, depth);
                        }

                        // If we get to here it means that we're a normal html tag.  No need to iterate any deeper into
                        // the children of it because they wont be tag helpers.
                    }
                    else
                    {
                        // We're not an Html tag so iterate through children recursively.
                        RewriteTags(childBlock, errorSink, depth + 1);
                        continue;
                    }
                }
                else
                {
                    ValidateParentAllowsContent((Span)child, errorSink);
                }

                // At this point the child is a Span or Block with Type BlockType.Tag that doesn't happen to be a
                // tag helper.

                // Add the child to current block.
                _currentBlock.Children.Add(child);
            }

            // We captured the number of active tag helpers at the start of our logic, it should be the same. If not
            // it means that there are malformed tag helpers at the top of our stack.
            if (activeTrackers != _trackerStack.Count)
            {
                // Malformed tag helpers built here will be tag helpers that do not have end tags in the current block
                // scope. Block scopes are special cases in Razor such as @<p> would cause an error because there's no
                // matching end </p> tag in the template block scope and therefore doesn't make sense as a tag helper.
                BuildMalformedTagHelpers(_trackerStack.Count - activeTrackers, errorSink);

                Debug.Assert(activeTrackers == _trackerStack.Count);
            }

            BuildCurrentlyTrackedBlock();
        }

        private void TrackTagBlock(Block childBlock, int depth)
        {
            var tagName = GetTagName(childBlock);

            // Don't want to track incomplete tags that have no tag name.
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return;
            }

            if (IsEndTag(childBlock))
            {
                var parentTracker = _trackerStack.Count > 0 ? _trackerStack.Peek() : null;
                if (parentTracker != null &&
                    !parentTracker.IsTagHelper &&
                    depth == parentTracker.Depth &&
                    string.Equals(parentTracker.TagName, tagName, StringComparison.OrdinalIgnoreCase))
                {
                    PopTrackerStack();
                }
            }
            else if (!VoidElements.Contains(tagName) && !IsSelfClosing(childBlock))
            {
                // If it's not a void element and it's not self-closing then we need to create a tag
                // tracker for it.
                var tracker = new TagBlockTracker(tagName, isTagHelper: false, depth: depth);
                PushTrackerStack(tracker);
            }
        }

        private bool TryRewriteTagHelper(Block tagBlock, ErrorSink errorSink)
        {
            // Get tag name of the current block (doesn't matter if it's an end or start tag)
            var tagName = GetTagName(tagBlock);

            // Could not determine tag name, it can't be a TagHelper, continue on and track the element.
            if (tagName == null)
            {
                return false;
            }

            TagHelperBinding tagHelperBinding;

            if (!IsPotentialTagHelper(tagName, tagBlock))
            {
                return false;
            }

            var tracker = _currentTagHelperTracker;
            var tagNameScope = tracker?.TagName ?? string.Empty;

            if (!IsEndTag(tagBlock))
            {
                // We're now in a start tag block, we first need to see if the tag block is a tag helper.
                var elementAttributes = GetAttributeNameValuePairs(tagBlock);

                tagHelperBinding = _tagHelperBinder.GetBinding(
                    tagName,
                    elementAttributes,
                    CurrentParentTagName,
                    CurrentParentIsTagHelper);

                // If there aren't any TagHelperDescriptors registered then we aren't a TagHelper
                if (tagHelperBinding == null)
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

                ValidateParentAllowsTagHelper(tagName, tagBlock, errorSink);
                ValidateBinding(tagHelperBinding, tagName, tagBlock, errorSink);

                // We're in a start TagHelper block.
                var validTagStructure = ValidateTagSyntax(tagName, tagBlock, errorSink);

                var builder = TagHelperBlockRewriter.Rewrite(
                    tagName,
                    validTagStructure,
                    _featureFlags,
                    tagBlock,
                    tagHelperBinding,
                    errorSink);

                // Track the original start tag so the editor knows where each piece of the TagHelperBlock lies
                // for formatting.
                builder.SourceStartTag = tagBlock;

                // Found a new tag helper block
                TrackTagHelperBlock(builder);

                // If it's a non-content expecting block then we don't have to worry about nested children within the
                // tag. Complete it.
                if (builder.TagMode == TagMode.SelfClosing || builder.TagMode == TagMode.StartTagOnly)
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

                    ValidateTagSyntax(tagName, tagBlock, errorSink);

                    BuildCurrentlyTrackedTagHelperBlock(tagBlock);
                }
                else
                {
                    tagHelperBinding = _tagHelperBinder.GetBinding(
                        tagName,
                        attributes: Array.Empty<KeyValuePair<string, string>>(),
                        parentTagName: CurrentParentTagName,
                        parentIsTagHelper: CurrentParentIsTagHelper);

                    // If there are not TagHelperDescriptors associated with the end tag block that also have no
                    // required attributes then it means we can't be a TagHelper, bail out.
                    if (tagHelperBinding == null)
                    {
                        return false;
                    }

                    foreach (var descriptor in tagHelperBinding.Descriptors)
                    {
                        var boundRules = tagHelperBinding.GetBoundRules(descriptor);
                        var invalidRule = boundRules.FirstOrDefault(rule => rule.TagStructure == TagStructure.WithoutEndTag);

                        if (invalidRule != null)
                        {
                            // End tag TagHelper that states it shouldn't have an end tag.
                            errorSink.OnError(
                                RazorDiagnosticFactory.CreateParsing_TagHelperMustNotHaveAnEndTag(
                                    new SourceSpan(SourceLocationTracker.Advance(tagBlock.Start, "</"), tagName.Length),
                                    tagName,
                                    descriptor.DisplayName,
                                    invalidRule.TagStructure));

                            return false;
                        }
                    }

                    // Current tag helper scope does not match the end tag. Attempt to recover the tag
                    // helper by looking up the previous tag helper scopes for a matching tag. If we
                    // can't recover it means there was no corresponding tag helper start tag.
                    if (TryRecoverTagHelper(tagName, tagBlock, errorSink))
                    {
                        ValidateParentAllowsTagHelper(tagName, tagBlock, errorSink);
                        ValidateTagSyntax(tagName, tagBlock, errorSink);

                        // Successfully recovered, move onto the next element.
                    }
                    else
                    {
                        // Could not recover, the end tag helper has no corresponding start tag, create
                        // an error based on the current childBlock.
                        errorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(SourceLocationTracker.Advance(tagBlock.Start, "</"), tagName.Length), tagName));

                        return false;
                    }
                }
            }

            return true;
        }

        // Internal for testing
        internal IReadOnlyList<KeyValuePair<string, string>> GetAttributeNameValuePairs(Block tagBlock)
        {
            // Need to calculate how many children we should take that represent the attributes.
            var childrenOffset = IsPartialTag(tagBlock) ? 0 : 1;
            var childCount = tagBlock.Children.Count - childrenOffset;

            if (childCount <= 1)
            {
                return Array.Empty<KeyValuePair<string, string>>();
            }

            _htmlAttributeTracker.Clear();

            var attributes = _htmlAttributeTracker;

            for (var i = 1; i < childCount; i++)
            {
                var child = tagBlock.Children[i];
                Span childSpan;

                if (child.IsBlock)
                {
                    var childBlock = (Block)child;

                    if (childBlock.Type != BlockKindInternal.Markup)
                    {
                        // Anything other than markup blocks in the attribute area of tags mangles following attributes.
                        // It's also not supported by TagHelpers, bail early to avoid creating bad attribute value pairs.
                        break;
                    }

                    childSpan = childBlock.FindFirstDescendentSpan();

                    if (childSpan == null)
                    {
                        _attributeValueBuilder.Append(InvalidAttributeValueMarker);
                        continue;
                    }

                    // We can assume the first span will always contain attributename=" and the last span will always
                    // contain the final quote. Therefore, if the values not quoted there's no ending quote to skip.
                    var childOffset = 0;
                    if (childSpan.Tokens.Count > 0)
                    {
                        var potentialQuote = childSpan.Tokens[childSpan.Tokens.Count - 1];
                        if (potentialQuote != null &&
                            (potentialQuote.Kind == SyntaxKind.DoubleQuote ||
                            potentialQuote.Kind == SyntaxKind.SingleQuote))
                        {
                            childOffset = 1;
                        }
                    }

                    for (var j = 1; j < childBlock.Children.Count - childOffset; j++)
                    {
                        var valueChild = childBlock.Children[j];
                        if (valueChild.IsBlock)
                        {
                            _attributeValueBuilder.Append(InvalidAttributeValueMarker);
                        }
                        else
                        {
                            var valueChildSpan = (Span)valueChild;
                            for (var k = 0; k < valueChildSpan.Tokens.Count; k++)
                            {
                                _attributeValueBuilder.Append(valueChildSpan.Tokens[k].Content);
                            }
                        }
                    }
                }
                else
                {
                    childSpan = (Span)child;

                    var afterEquals = false;
                    var atValue = false;
                    var endValueMarker = childSpan.Tokens.Count;

                    // Entire attribute is a string
                    for (var j = 0; j < endValueMarker; j++)
                    {
                        var token = childSpan.Tokens[j];

                        if (!afterEquals)
                        {
                            afterEquals = token.Kind == SyntaxKind.Equals;
                            continue;
                        }

                        if (!atValue)
                        {
                            atValue = token.Kind != SyntaxKind.Whitespace &&
                                token.Kind != SyntaxKind.NewLine;

                            if (atValue)
                            {
                                if (token.Kind == SyntaxKind.DoubleQuote ||
                                    token.Kind == SyntaxKind.SingleQuote)
                                {
                                    endValueMarker--;
                                }
                                else
                                {
                                    // Current token is considered the value (unquoted). Add its content to the
                                    // attribute value builder before we move past it.
                                    _attributeValueBuilder.Append(token.Content);
                                }
                            }

                            continue;
                        }

                        _attributeValueBuilder.Append(token.Content);
                    }
                }

                var start = 0;
                for (; start < childSpan.Content.Length; start++)
                {
                    if (!char.IsWhiteSpace(childSpan.Content[start]))
                    {
                        break;
                    }
                }

                var end = start;
                for (; end < childSpan.Content.Length; end++)
                {
                    if (childSpan.Content[end] == '=')
                    {
                        break;
                    }
                }

                var attributeName = childSpan.Content.Substring(start, end - start);
                var attributeValue = _attributeValueBuilder.ToString();
                var attribute = new KeyValuePair<string, string>(attributeName, attributeValue);
                attributes.Add(attribute);

                _attributeValueBuilder.Clear();
            }

            return attributes;
        }

        private bool HasAllowedChildren()
        {
            var currentTracker = _trackerStack.Count > 0 ? _trackerStack.Peek() : null;

            // If the current tracker is not a TagHelper then there's no AllowedChildren to enforce.
            if (currentTracker == null || !currentTracker.IsTagHelper)
            {
                return false;
            }

            return _currentTagHelperTracker.AllowedChildren != null && _currentTagHelperTracker.AllowedChildren.Count > 0;
        }

        private void ValidateParentAllowsContent(Span child, ErrorSink errorSink)
        {
            if (HasAllowedChildren())
            {
                var isDisallowedContent = true;
                if (_featureFlags.AllowHtmlCommentsInTagHelpers)
                {
                    isDisallowedContent = !IsComment(child) && child.Kind != SpanKindInternal.Transition && child.Kind != SpanKindInternal.Code;
                }

                if (isDisallowedContent)
                {
                    var content = child.Content;
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var trimmedStart = content.TrimStart();
                        var whitespace = content.Substring(0, content.Length - trimmedStart.Length);
                        var errorStart = SourceLocationTracker.Advance(child.Start, whitespace);
                        var length = trimmedStart.TrimEnd().Length;
                        var allowedChildren = _currentTagHelperTracker.AllowedChildren;
                        var allowedChildrenString = string.Join(", ", allowedChildren);
                        errorSink.OnError(
                            RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                                new SourceSpan(errorStart, length),
                                _currentTagHelperTracker.TagName,
                                allowedChildrenString));
                    }
                }
            }
        }

        private void ValidateParentAllowsPlainTag(Block tagBlock, ErrorSink errorSink)
        {
            var tagName = GetTagName(tagBlock);

            // Treat partial tags such as '</' which have no tag names as content.
            if (string.IsNullOrEmpty(tagName))
            {
                Debug.Assert(tagBlock.Children.First() is Span);

                ValidateParentAllowsContent((Span)tagBlock.Children.First(), errorSink);
                return;
            }

            if (!HasAllowedChildren())
            {
                return;
            }

            var tagHelperBinding = _tagHelperBinder.GetBinding(
                tagName,
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: CurrentParentTagName,
                parentIsTagHelper: CurrentParentIsTagHelper);

            // If we found a binding for the current tag, then it is a tag helper. Use the prefixed allowed children to compare.
            var allowedChildren = tagHelperBinding != null ? _currentTagHelperTracker.PrefixedAllowedChildren : _currentTagHelperTracker.AllowedChildren;
            if (!allowedChildren.Contains(tagName, StringComparer.OrdinalIgnoreCase))
            {
                OnAllowedChildrenTagError(_currentTagHelperTracker, tagName, tagBlock, errorSink);
            }
        }

        private void ValidateParentAllowsTagHelper(string tagName, Block tagBlock, ErrorSink errorSink)
        {
            if (HasAllowedChildren() &&
                !_currentTagHelperTracker.PrefixedAllowedChildren.Contains(tagName, StringComparer.OrdinalIgnoreCase))
            {
                OnAllowedChildrenTagError(_currentTagHelperTracker, tagName, tagBlock, errorSink);
            }
        }

        private static void OnAllowedChildrenTagError(
            TagHelperBlockTracker tracker,
            string tagName,
            Block tagBlock,
            ErrorSink errorSink)
        {
            var allowedChildrenString = string.Join(", ", tracker.AllowedChildren);
            var errorStart = GetTagDeclarationErrorStart(tagBlock);

            errorSink.OnError(
                RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                    new SourceSpan(errorStart, tagName.Length),
                    tagName,
                    tracker.TagName,
                    allowedChildrenString));
        }

        private static void ValidateBinding(
            TagHelperBinding bindingResult,
            string tagName,
            Block tagBlock,
            ErrorSink errorSink)
        {
            // Ensure that all descriptors associated with this tag have appropriate TagStructures. Cannot have
            // multiple descriptors that expect different TagStructures (other than TagStructure.Unspecified).
            TagHelperDescriptor baseDescriptor = null;
            TagStructure? baseStructure = null;
            foreach (var descriptor in bindingResult.Descriptors)
            {
                var boundRules = bindingResult.GetBoundRules(descriptor);
                foreach (var rule in boundRules)
                {
                    if (rule.TagStructure != TagStructure.Unspecified)
                    {
                        // Can't have a set of TagHelpers that expect different structures.
                        if (baseStructure.HasValue && baseStructure != rule.TagStructure)
                        {
                            errorSink.OnError(
                                RazorDiagnosticFactory.CreateTagHelper_InconsistentTagStructure(
                                    new SourceSpan(tagBlock.Start, tagBlock.Length),
                                    baseDescriptor.DisplayName,
                                    descriptor.DisplayName,
                                    tagName));
                        }

                        baseDescriptor = descriptor;
                        baseStructure = rule.TagStructure;
                    }
                }
            }
        }

        private static bool ValidateTagSyntax(string tagName, Block tag, ErrorSink errorSink)
        {
            // We assume an invalid syntax until we verify that the tag meets all of our "valid syntax" criteria.
            if (IsPartialTag(tag))
            {
                var errorStart = GetTagDeclarationErrorStart(tag);

                errorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                        new SourceSpan(errorStart, tagName.Length), tagName));

                return false;
            }

            return true;
        }

        private static SourceLocation GetTagDeclarationErrorStart(Block tagBlock)
        {
            var advanceBy = IsEndTag(tagBlock) ? "</" : "<";

            return SourceLocationTracker.Advance(tagBlock.Start, advanceBy);
        }

        private static bool IsPartialTag(Block tagBlock)
        {
            // No need to validate the tag end because in order to be a tag block it must start with '<'.
            var tagEnd = tagBlock.Children[tagBlock.Children.Count - 1] as Span;

            // If our tag end is not a markup span it means it's some sort of code SyntaxTreeNode (not a valid format)
            if (tagEnd != null && tagEnd.Kind == SpanKindInternal.Markup)
            {
                var endToken = tagEnd.Tokens.Count > 0 ?
                    tagEnd.Tokens[tagEnd.Tokens.Count - 1] :
                    null;

                if (endToken != null && endToken.Kind == SyntaxKind.CloseAngle)
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
            Debug.Assert(_trackerStack.Any(tracker => tracker.IsTagHelper));

            // We need to pop all trackers until we reach our TagHelperBlock. We can throw away any non-TagHelper
            // trackers because they don't need to be well-formed.
            TagHelperBlockTracker tagHelperTracker;
            do
            {
                tagHelperTracker = PopTrackerStack() as TagHelperBlockTracker;
            }
            while (tagHelperTracker == null);

            // Track the original end tag so the editor knows where each piece of the TagHelperBlock lies
            // for formatting.
            tagHelperTracker.Builder.SourceEndTag = endTag;

            _currentTagHelperTracker =
                (TagHelperBlockTracker)_trackerStack.FirstOrDefault(tagBlockTracker => tagBlockTracker.IsTagHelper);

            BuildCurrentlyTrackedBlock();
        }

        private bool IsPotentialTagHelper(string tagName, Block childBlock)
        {
            Debug.Assert(childBlock.Children.Count > 0);
            var child = childBlock.Children[0];

            var childSpan = (Span)child;

            // text tags that are labeled as transitions should be ignored aka they're not tag helpers.
            return !string.Equals(tagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase) ||
                   childSpan.Kind != SpanKindInternal.Transition;
        }

        private void TrackBlock(BlockBuilder builder)
        {
            _currentBlock = builder;

            _blockStack.Push(builder);
        }

        private void TrackTagHelperBlock(TagHelperBlockBuilder builder)
        {
            _currentTagHelperTracker = new TagHelperBlockTracker(_tagHelperPrefix, builder);
            PushTrackerStack(_currentTagHelperTracker);

            TrackBlock(builder);
        }

        private bool TryRecoverTagHelper(string tagName, Block endTag, ErrorSink errorSink)
        {
            var malformedTagHelperCount = 0;

            foreach (var tracker in _trackerStack)
            {
                if (tracker.IsTagHelper && tracker.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                malformedTagHelperCount++;
            }

            // If the malformedTagHelperCount == _tagStack.Count it means we couldn't find a start tag for the tag
            // helper, can't recover.
            if (malformedTagHelperCount != _trackerStack.Count)
            {
                BuildMalformedTagHelpers(malformedTagHelperCount, errorSink);

                // One final build, this is the build that completes our target tag helper block which is not malformed.
                BuildCurrentlyTrackedTagHelperBlock(endTag);

                // We were able to recover
                return true;
            }

            // Could not recover tag helper. Aka we found a tag helper end tag without a corresponding start tag.
            return false;
        }

        private void BuildMalformedTagHelpers(int count, ErrorSink errorSink)
        {
            for (var i = 0; i < count; i++)
            {
                var tracker = _trackerStack.Peek();

                // Skip all non-TagHelper entries. Non TagHelper trackers do not need to represent well-formed HTML.
                if (!tracker.IsTagHelper)
                {
                    PopTrackerStack();
                    continue;
                }

                var malformedTagHelper = ((TagHelperBlockTracker)tracker).Builder;

                errorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                        new SourceSpan(SourceLocationTracker.Advance(malformedTagHelper.Start, "<"), malformedTagHelper.TagName.Length),
                        malformedTagHelper.TagName));

                BuildCurrentlyTrackedTagHelperBlock(endTag: null);
            }
        }

        private static string GetTagName(Block tagBlock)
        {
            var child = tagBlock.Children[0];

            if (tagBlock.Type != BlockKindInternal.Tag || tagBlock.Children.Count == 0 || !(child is Span))
            {
                return null;
            }

            var childSpan = (Span)child;
            SyntaxToken textToken = null;
            for (var i = 0; i < childSpan.Tokens.Count; i++)
            {
                var token = childSpan.Tokens[i];

                if (token != null &&
                    (token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.Text))
                {
                    textToken = token;
                    break;
                }
            }

            if (textToken == null)
            {
                return null;
            }

            return textToken.Kind == SyntaxKind.Whitespace ? null : textToken.Content;
        }

        private static bool IsEndTag(Block tagBlock)
        {
            EnsureTagBlock(tagBlock);

            var childSpan = (Span)tagBlock.Children.First();

            // We grab the token that could be forward slash
            var relevantToken = childSpan.Tokens[childSpan.Tokens.Count == 1 ? 0 : 1];

            return relevantToken.Kind == SyntaxKind.ForwardSlash;
        }

        internal static bool IsComment(Span span)
        {
            Block currentBlock = span.Parent;
            while (currentBlock != null && currentBlock.Type != BlockKindInternal.Comment && currentBlock.Type != BlockKindInternal.HtmlComment)
            {
                currentBlock = currentBlock.Parent;
            }

            return currentBlock != null;
        }


        private static void EnsureTagBlock(Block tagBlock)
        {
            Debug.Assert(tagBlock.Type == BlockKindInternal.Tag);
            Debug.Assert(tagBlock.Children.First() is Span);
        }

        private static bool IsSelfClosing(Block childBlock)
        {
            var childSpan = childBlock.FindLastDescendentSpan();

            return childSpan?.Content.EndsWith("/>", StringComparison.Ordinal) ?? false;
        }

        private void PushTrackerStack(TagBlockTracker tracker)
        {
            _trackerStack.Push(tracker);
        }

        private TagBlockTracker PopTrackerStack()
        {
            var poppedTracker = _trackerStack.Pop();

            return poppedTracker;
        }

        private class TagBlockTracker
        {
            public TagBlockTracker(string tagName, bool isTagHelper, int depth)
            {
                TagName = tagName;
                IsTagHelper = isTagHelper;
                Depth = depth;
            }

            public string TagName { get; }

            public bool IsTagHelper { get; }

            public int Depth { get; }
        }

        private class TagHelperBlockTracker : TagBlockTracker
        {
            private IReadOnlyList<string> _prefixedAllowedChildren;
            private readonly string _tagHelperPrefix;

            public TagHelperBlockTracker(string tagHelperPrefix, TagHelperBlockBuilder builder)
                : base(builder.TagName, isTagHelper: true, depth: 0)
            {
                _tagHelperPrefix = tagHelperPrefix;
                Builder = builder;

                if (Builder.BindingResult.Descriptors.Any(descriptor => descriptor.AllowedChildTags != null))
                {
                    AllowedChildren = Builder.BindingResult.Descriptors
                        .Where(descriptor => descriptor.AllowedChildTags != null)
                        .SelectMany(descriptor => descriptor.AllowedChildTags.Select(childTag => childTag.Name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }

            public TagHelperBlockBuilder Builder { get; }

            public uint OpenMatchingTags { get; set; }

            public IReadOnlyList<string> AllowedChildren { get; }

            public IReadOnlyList<string> PrefixedAllowedChildren
            {
                get
                {
                    if (AllowedChildren != null && _prefixedAllowedChildren == null)
                    {
                        Debug.Assert(Builder.BindingResult.Descriptors.Count() >= 1);

                        _prefixedAllowedChildren = AllowedChildren.Select(allowedChild => _tagHelperPrefix + allowedChild).ToList();
                    }

                    return _prefixedAllowedChildren;
                }
            }
        }
    }
}