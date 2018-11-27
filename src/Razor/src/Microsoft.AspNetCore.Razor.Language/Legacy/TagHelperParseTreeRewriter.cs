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
        public static RazorSyntaxTree Rewrite(RazorSyntaxTree syntaxTree, string tagHelperPrefix, IEnumerable<TagHelperDescriptor> descriptors)
        {
            var errorSink = new ErrorSink();
            syntaxTree = MarkupElementRewriter.AddMarkupElements(syntaxTree);

            var rewriter = new Rewriter(
                syntaxTree.Source,
                tagHelperPrefix,
                descriptors,
                syntaxTree.Options.FeatureFlags,
                errorSink);

            var rewritten = rewriter.Visit(syntaxTree.Root);

            var errorList = new List<RazorDiagnostic>();
            errorList.AddRange(errorSink.Errors);
            errorList.AddRange(descriptors.SelectMany(d => d.GetAllDiagnostics()));

            var diagnostics = CombineErrors(syntaxTree.Diagnostics, errorList).OrderBy(error => error.Span.AbsoluteIndex);

            var newSyntaxTree = RazorSyntaxTree.Create(rewritten, syntaxTree.Source, diagnostics, syntaxTree.Options);
            newSyntaxTree = MarkupElementRewriter.RemoveMarkupElements(newSyntaxTree);

            return newSyntaxTree;
        }

        private static IReadOnlyList<RazorDiagnostic> CombineErrors(IReadOnlyList<RazorDiagnostic> errors1, IReadOnlyList<RazorDiagnostic> errors2)
        {
            var combinedErrors = new List<RazorDiagnostic>(errors1.Count + errors2.Count);
            combinedErrors.AddRange(errors1);
            combinedErrors.AddRange(errors2);

            return combinedErrors;
        }

        // Internal for testing.
        internal class Rewriter : SyntaxRewriter
        {
            // Internal for testing.
            // Null characters are invalid markup for HTML attribute values.
            internal static readonly string InvalidAttributeValueMarker = "\0";

            private readonly RazorSourceDocument _source;
            private readonly string _tagHelperPrefix;
            private readonly List<KeyValuePair<string, string>> _htmlAttributeTracker;
            private readonly StringBuilder _attributeValueBuilder;
            private readonly TagHelperBinder _tagHelperBinder;
            private readonly Stack<TagTracker> _trackerStack;
            private readonly ErrorSink _errorSink;
            private RazorParserFeatureFlags _featureFlags;

            public Rewriter(
                RazorSourceDocument source,
                string tagHelperPrefix,
                IEnumerable<TagHelperDescriptor> descriptors,
                RazorParserFeatureFlags featureFlags,
                ErrorSink errorSink)
            {
                _source = source;
                _tagHelperPrefix = tagHelperPrefix;
                _tagHelperBinder = new TagHelperBinder(tagHelperPrefix, descriptors);
                _trackerStack = new Stack<TagTracker>();
                _attributeValueBuilder = new StringBuilder();
                _htmlAttributeTracker = new List<KeyValuePair<string, string>>();
                _featureFlags = featureFlags;
                _errorSink = errorSink;
            }

            private TagTracker CurrentTracker => _trackerStack.Count > 0 ? _trackerStack.Peek() : null;

            private string CurrentParentTagName => CurrentTracker?.TagName;

            private bool CurrentParentIsTagHelper => CurrentTracker?.IsTagHelper ?? false;

            private TagHelperTracker CurrentTagHelperTracker => _trackerStack.FirstOrDefault(t => t.IsTagHelper) as TagHelperTracker;

            public override SyntaxNode VisitMarkupElement(MarkupElementSyntax node)
            {
                if (IsPartOfStartTag(node))
                {
                    // If this element is inside a start tag, it is some sort of malformed case like
                    // <p @do { someattribute=\"btn\"></p>, where the end "p" tag is inside the start "p" tag.
                    // We don't want to do tag helper parsing for this tag.
                    return base.VisitMarkupElement(node);
                }

                MarkupTagHelperStartTagSyntax tagHelperStart = null;
                MarkupTagHelperEndTagSyntax tagHelperEnd = null;
                TagHelperInfo tagHelperInfo = null;

                // Visit the start tag.
                var startTag = (MarkupTagBlockSyntax)Visit(node.StartTag);
                if (startTag != null)
                {
                    var tagName = startTag.GetTagName();
                    if (TryRewriteTagHelperStart(startTag, out tagHelperStart, out tagHelperInfo))
                    {
                        // This is a tag helper.
                        if (tagHelperInfo.TagMode == TagMode.SelfClosing || tagHelperInfo.TagMode == TagMode.StartTagOnly)
                        {
                            var tagHelperElement = SyntaxFactory.MarkupTagHelperElement(tagHelperStart, body: new SyntaxList<RazorSyntaxNode>(), endTag: null);
                            var rewrittenTagHelper = tagHelperElement.WithTagHelperInfo(tagHelperInfo);
                            if (node.Body.Count == 0)
                            {
                                return rewrittenTagHelper;
                            }

                            // This tag contains a body which needs to be moved to the parent.
                            var rewrittenNodes = SyntaxListBuilder<RazorSyntaxNode>.Create();
                            rewrittenNodes.Add(rewrittenTagHelper);
                            var rewrittenBody = VisitList(node.Body);
                            rewrittenNodes.AddRange(rewrittenBody);

                            return SyntaxFactory.MarkupElement(startTag: null, body: rewrittenNodes.ToList(), endTag: null);
                        }
                        else if (node.EndTag == null)
                        {
                            // Start tag helper with no corresponding end tag.
                            _errorSink.OnError(
                                RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                    new SourceSpan(SourceLocationTracker.Advance(startTag.GetSourceLocation(_source), "<"), tagName.Length),
                                    tagName));
                        }
                        else
                        {
                            // Tag helper start tag. Keep track.
                            var tracker = new TagHelperTracker(_tagHelperPrefix, tagHelperInfo);
                            _trackerStack.Push(tracker);
                        }
                    }
                    else
                    {
                        // Non-TagHelper tag.
                        ValidateParentAllowsPlainTag(startTag);

                        if (!startTag.IsSelfClosing() && !startTag.IsVoidElement())
                        {
                            var tracker = new TagTracker(tagName, isTagHelper: false);
                            _trackerStack.Push(tracker);
                        }
                    }
                }

                // Visit body between start and end tags.
                var body = VisitList(node.Body);

                // Visit end tag.
                var endTag = (MarkupTagBlockSyntax)Visit(node.EndTag);
                if (endTag != null)
                {
                    var tagName = endTag.GetTagName();
                    if (TryRewriteTagHelperEnd(endTag, out tagHelperEnd))
                    {
                        // This is a tag helper
                        if (startTag == null)
                        {
                            // The end tag helper has no corresponding start tag, create an error.
                            _errorSink.OnError(
                                RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                    new SourceSpan(SourceLocationTracker.Advance(endTag.GetSourceLocation(_source), "</"), tagName.Length), tagName));
                        }
                    }
                    else
                    {
                        // Non tag helper end tag.
                        if (startTag == null)
                        {
                            // Standalone end tag. We may need to error if it is not supposed to be here.
                            // If there was a corresponding start tag, we would have already added this error.
                            ValidateParentAllowsPlainTag(endTag);
                        }
                        else if (!endTag.IsVoidElement())
                        {
                            // Since a start tag exists, we must already be tracking it.
                            // Pop the stack as we're done with the end tag.
                            _trackerStack.Pop();
                        }
                    }
                }

                if (tagHelperInfo != null)
                {
                    // If we get here it means this element was rewritten as a tag helper.
                    var tagHelperElement = SyntaxFactory.MarkupTagHelperElement(tagHelperStart, body, tagHelperEnd);
                    return tagHelperElement.WithTagHelperInfo(tagHelperInfo);
                }

                // There was no matching tag helper for this element. Return.
                return node.Update(startTag, body, endTag);
            }

            public override SyntaxNode VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
            {
                var tagParent = node.FirstAncestorOrSelf<SyntaxNode>(n => n is MarkupTagBlockSyntax);
                var isPartofTagBlock = tagParent != null;
                if (!isPartofTagBlock)
                {
                    ValidateParentAllowsContent(node);
                }

                return base.VisitMarkupTextLiteral(node);
            }

            private bool TryRewriteTagHelperStart(MarkupTagBlockSyntax tagBlock, out MarkupTagHelperStartTagSyntax rewritten, out TagHelperInfo tagHelperInfo)
            {
                rewritten = null;
                tagHelperInfo = null;

                // Get tag name of the current block
                var tagName = tagBlock.GetTagName();

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

                var tracker = CurrentTagHelperTracker;
                var tagNameScope = tracker?.TagName ?? string.Empty;

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

                ValidateParentAllowsTagHelper(tagName, tagBlock);
                ValidateBinding(tagHelperBinding, tagName, tagBlock);

                // We're in a start TagHelper block.
                var validTagStructure = ValidateTagSyntax(tagName, tagBlock);

                var startTag = TagHelperBlockRewriter.Rewrite(
                    tagName,
                    validTagStructure,
                    _featureFlags,
                    tagBlock,
                    tagHelperBinding,
                    _errorSink,
                    _source);

                var tagMode = TagHelperBlockRewriter.GetTagMode(tagBlock, tagHelperBinding, _errorSink);
                tagHelperInfo = new TagHelperInfo(tagName, tagMode, tagHelperBinding);
                rewritten = startTag;

                return true;
            }

            private bool TryRewriteTagHelperEnd(MarkupTagBlockSyntax tagBlock, out MarkupTagHelperEndTagSyntax rewritten)
            {
                rewritten = null;
                var tagName = tagBlock.GetTagName();
                // Could not determine tag name, it can't be a TagHelper, continue on and track the element.
                if (tagName == null)
                {
                    return false;
                }

                var tracker = CurrentTagHelperTracker;
                var tagNameScope = tracker?.TagName ?? string.Empty;
                if (!IsPotentialTagHelper(tagName, tagBlock))
                {
                    return false;
                }

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

                    ValidateTagSyntax(tagName, tagBlock);

                    _trackerStack.Pop();
                }
                else
                {
                    var tagHelperBinding = _tagHelperBinder.GetBinding(
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
                            _errorSink.OnError(
                                RazorDiagnosticFactory.CreateParsing_TagHelperMustNotHaveAnEndTag(
                                    new SourceSpan(SourceLocationTracker.Advance(tagBlock.GetSourceLocation(_source), "</"), tagName.Length),
                                    tagName,
                                    descriptor.DisplayName,
                                    invalidRule.TagStructure));

                            return false;
                        }
                    }
                }

                rewritten = SyntaxFactory.MarkupTagHelperEndTag(tagBlock.Children);

                return true;
            }

            // Internal for testing
            internal IReadOnlyList<KeyValuePair<string, string>> GetAttributeNameValuePairs(MarkupTagBlockSyntax tagBlock)
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
                    if (tagBlock.Children[i] is CSharpCodeBlockSyntax)
                    {
                        // Code blocks in the attribute area of tags mangles following attributes.
                        // It's also not supported by TagHelpers, bail early to avoid creating bad attribute value pairs.
                        break;
                    }

                    if (tagBlock.Children[i] is MarkupMinimizedAttributeBlockSyntax minimizedAttributeBlock)
                    {
                        if (minimizedAttributeBlock.Name == null)
                        {
                            _attributeValueBuilder.Append(InvalidAttributeValueMarker);
                            continue;
                        }

                        var minimizedAttribute = new KeyValuePair<string, string>(minimizedAttributeBlock.Name.GetContent(), string.Empty);
                        attributes.Add(minimizedAttribute);
                        continue;
                    }

                    if (!(tagBlock.Children[i] is MarkupAttributeBlockSyntax attributeBlock))
                    {
                        // If the parser thought these aren't attributes, we don't care about them. Move on.
                        continue;
                    }

                    if (attributeBlock.Name == null)
                    {
                        _attributeValueBuilder.Append(InvalidAttributeValueMarker);
                        continue;
                    }

                    if (attributeBlock.Value != null)
                    {
                        for (var j = 0; j < attributeBlock.Value.Children.Count; j++)
                        {
                            var child = attributeBlock.Value.Children[j];
                            if (child is MarkupLiteralAttributeValueSyntax literalValue)
                            {
                                _attributeValueBuilder.Append(literalValue.GetContent());
                            }
                            else
                            {
                                _attributeValueBuilder.Append(InvalidAttributeValueMarker);
                            }
                        }
                    }

                    var attributeName = attributeBlock.Name.GetContent();
                    var attributeValue = _attributeValueBuilder.ToString();
                    var attribute = new KeyValuePair<string, string>(attributeName, attributeValue);
                    attributes.Add(attribute);

                    _attributeValueBuilder.Clear();
                }

                return attributes;
            }

            private void ValidateParentAllowsTagHelper(string tagName, MarkupTagBlockSyntax tagBlock)
            {
                if (HasAllowedChildren() &&
                    !CurrentTagHelperTracker.PrefixedAllowedChildren.Contains(tagName, StringComparer.OrdinalIgnoreCase))
                {
                    OnAllowedChildrenTagError(CurrentTagHelperTracker, tagName, tagBlock, _errorSink, _source);
                }
            }

            private void ValidateBinding(
                TagHelperBinding bindingResult,
                string tagName,
                MarkupTagBlockSyntax tagBlock)
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
                                _errorSink.OnError(
                                    RazorDiagnosticFactory.CreateTagHelper_InconsistentTagStructure(
                                        new SourceSpan(tagBlock.GetSourceLocation(_source), tagBlock.FullWidth),
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

            private bool ValidateTagSyntax(string tagName, MarkupTagBlockSyntax tag)
            {
                // We assume an invalid syntax until we verify that the tag meets all of our "valid syntax" criteria.
                if (IsPartialTag(tag))
                {
                    var errorStart = GetTagDeclarationErrorStart(tag);

                    _errorSink.OnError(
                        RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                            new SourceSpan(errorStart, tagName.Length), tagName));

                    return false;
                }

                return true;
            }

            private bool IsPotentialTagHelper(string tagName, MarkupTagBlockSyntax childBlock)
            {
                Debug.Assert(childBlock.Children.Count > 0);
                var child = childBlock.Children[0];

                return !string.Equals(tagName, SyntaxConstants.TextTagName, StringComparison.OrdinalIgnoreCase) ||
                       child.Kind != SyntaxKind.MarkupTransition;
            }

            private SourceLocation GetTagDeclarationErrorStart(MarkupTagBlockSyntax tagBlock)
            {
                var advanceBy = IsEndTag(tagBlock) ? "</" : "<";

                return SourceLocationTracker.Advance(tagBlock.GetSourceLocation(_source), advanceBy);
            }

            private static bool IsPartialTag(MarkupTagBlockSyntax tagBlock)
            {
                // No need to validate the tag end because in order to be a tag block it must start with '<'.
                var tagEnd = tagBlock.Children[tagBlock.Children.Count - 1];

                // If our tag end is not a markup span it means it's some sort of code SyntaxTreeNode (not a valid format)
                if (tagEnd != null && tagEnd is MarkupTextLiteralSyntax tagEndLiteral)
                {
                    var endToken = tagEndLiteral.LiteralTokens.Count > 0 ?
                        tagEndLiteral.LiteralTokens[tagEndLiteral.LiteralTokens.Count - 1] :
                        null;

                    if (endToken != null && endToken.Kind == SyntaxKind.CloseAngle)
                    {
                        return false;
                    }
                }

                return true;
            }

            private void ValidateParentAllowsContent(SyntaxNode child)
            {
                if (HasAllowedChildren())
                {
                    var isDisallowedContent = true;
                    if (_featureFlags.AllowHtmlCommentsInTagHelpers)
                    {
                        isDisallowedContent = !IsComment(child) &&
                            !child.IsTransitionSpanKind() &&
                            !child.IsCodeSpanKind();
                    }

                    if (isDisallowedContent)
                    {
                        var content = child.GetContent();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            var trimmedStart = content.TrimStart();
                            var whitespace = content.Substring(0, content.Length - trimmedStart.Length);
                            var errorStart = SourceLocationTracker.Advance(child.GetSourceLocation(_source), whitespace);
                            var length = trimmedStart.TrimEnd().Length;
                            var allowedChildren = CurrentTagHelperTracker.AllowedChildren;
                            var allowedChildrenString = string.Join(", ", allowedChildren);
                            _errorSink.OnError(
                                RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                                    new SourceSpan(errorStart, length),
                                    CurrentTagHelperTracker.TagName,
                                    allowedChildrenString));
                        }
                    }
                }
            }

            private void ValidateParentAllowsPlainTag(MarkupTagBlockSyntax tagBlock)
            {
                var tagName = tagBlock.GetTagName();

                // Treat partial tags such as '</' which have no tag names as content.
                if (string.IsNullOrEmpty(tagName))
                {
                    var firstChild = tagBlock.Children.First();
                    Debug.Assert(firstChild is MarkupTextLiteralSyntax || firstChild is MarkupTransitionSyntax);

                    ValidateParentAllowsContent(tagBlock.Children.First());
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
                var allowedChildren = tagHelperBinding != null ? CurrentTagHelperTracker.PrefixedAllowedChildren : CurrentTagHelperTracker.AllowedChildren;
                if (!allowedChildren.Contains(tagName, StringComparer.OrdinalIgnoreCase))
                {
                    OnAllowedChildrenTagError(CurrentTagHelperTracker, tagName, tagBlock, _errorSink, _source);
                }
            }

            private bool HasAllowedChildren()
            {
                // TODO: Questionable logic. Need to revisit
                var currentTracker = _trackerStack.Count > 0 ? _trackerStack.Peek() : null;

                // If the current tracker is not a TagHelper then there's no AllowedChildren to enforce.
                if (currentTracker == null || !currentTracker.IsTagHelper)
                {
                    return false;
                }

                return CurrentTagHelperTracker.AllowedChildren != null && CurrentTagHelperTracker.AllowedChildren.Count > 0;
            }

            private bool IsPartOfStartTag(SyntaxNode node)
            {
                // Check if an ancestor is a start tag of a MarkupElement.
                var parent = node.FirstAncestorOrSelf<SyntaxNode>(n =>
                {
                    return n.Parent is MarkupElementSyntax element && element.StartTag == n;
                });

                return parent != null;
            }

            internal static bool IsComment(SyntaxNode node)
            {
                var commentParent = node.FirstAncestorOrSelf<SyntaxNode>(
                    n => n is RazorCommentBlockSyntax || n is MarkupCommentBlockSyntax);

                return commentParent != null;
            }

            private static void OnAllowedChildrenTagError(
                TagHelperTracker tracker,
                string tagName,
                MarkupTagBlockSyntax tagBlock,
                ErrorSink errorSink,
                RazorSourceDocument source)
            {
                var allowedChildrenString = string.Join(", ", tracker.AllowedChildren);
                var errorStart = GetTagDeclarationErrorStart(tagBlock, source);

                errorSink.OnError(
                    RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                        new SourceSpan(errorStart, tagName.Length),
                        tagName,
                        tracker.TagName,
                        allowedChildrenString));
            }

            private static SourceLocation GetTagDeclarationErrorStart(MarkupTagBlockSyntax tagBlock, RazorSourceDocument source)
            {
                var advanceBy = IsEndTag(tagBlock) ? "</" : "<";

                return SourceLocationTracker.Advance(tagBlock.GetSourceLocation(source), advanceBy);
            }

            private static bool IsEndTag(MarkupTagBlockSyntax tagBlock)
            {
                var childSpan = (MarkupTextLiteralSyntax)tagBlock.Children.First();

                // We grab the token that could be forward slash
                var relevantToken = childSpan.LiteralTokens[childSpan.LiteralTokens.Count == 1 ? 0 : 1];

                return relevantToken.Kind == SyntaxKind.ForwardSlash;
            }

            private class TagTracker
            {
                public TagTracker(string tagName, bool isTagHelper)
                {
                    TagName = tagName;
                    IsTagHelper = isTagHelper;
                }

                public string TagName { get; }

                public bool IsTagHelper { get; }
            }

            private class TagHelperTracker : TagTracker
            {
                private IReadOnlyList<string> _prefixedAllowedChildren;
                private readonly string _tagHelperPrefix;

                public TagHelperTracker(string tagHelperPrefix, TagHelperInfo info)
                    : base(info.TagName, isTagHelper: true)
                {
                    _tagHelperPrefix = tagHelperPrefix;
                    Info = info;

                    if (Info.BindingResult.Descriptors.Any(descriptor => descriptor.AllowedChildTags != null))
                    {
                        AllowedChildren = Info.BindingResult.Descriptors
                            .Where(descriptor => descriptor.AllowedChildTags != null)
                            .SelectMany(descriptor => descriptor.AllowedChildTags.Select(childTag => childTag.Name))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    }
                }

                public TagHelperInfo Info { get; }

                public uint OpenMatchingTags { get; set; }

                public IReadOnlyList<string> AllowedChildren { get; }

                public IReadOnlyList<string> PrefixedAllowedChildren
                {
                    get
                    {
                        if (AllowedChildren != null && _prefixedAllowedChildren == null)
                        {
                            Debug.Assert(Info.BindingResult.Descriptors.Count() >= 1);

                            _prefixedAllowedChildren = AllowedChildren.Select(allowedChild => _tagHelperPrefix + allowedChild).ToList();
                        }

                        return _prefixedAllowedChildren;
                    }
                }
            }
        }
    }
}
