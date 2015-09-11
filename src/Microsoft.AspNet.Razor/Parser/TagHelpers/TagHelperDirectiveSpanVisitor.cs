// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Parser.TagHelpers
{
    /// <summary>
    /// A <see cref="ParserVisitor"/> that generates <see cref="TagHelperDescriptor"/>s from
    /// tag helper chunk generators in a Razor document.
    /// </summary>
    public class TagHelperDirectiveSpanVisitor : ParserVisitor
    {
        private readonly ITagHelperDescriptorResolver _descriptorResolver;
        private readonly ErrorSink _errorSink;

        private List<TagHelperDirectiveDescriptor> _directiveDescriptors;

        // Internal for testing use
        internal TagHelperDirectiveSpanVisitor(ITagHelperDescriptorResolver descriptorResolver)
            : this(descriptorResolver, new ErrorSink())
        {
        }

        public TagHelperDirectiveSpanVisitor(
            ITagHelperDescriptorResolver descriptorResolver,
            ErrorSink errorSink)
        {
            if (descriptorResolver == null)
            {
                throw new ArgumentNullException(nameof(descriptorResolver));
            }

            if (errorSink == null)
            {
                throw new ArgumentNullException(nameof(errorSink));
            }

            _descriptorResolver = descriptorResolver;
            _errorSink = errorSink;
        }

        public IEnumerable<TagHelperDescriptor> GetDescriptors(Block root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _directiveDescriptors = new List<TagHelperDirectiveDescriptor>();

            // This will recurse through the syntax tree.
            VisitBlock(root);

            var resolutionContext = GetTagHelperDescriptorResolutionContext(_directiveDescriptors, _errorSink);
            var descriptors = _descriptorResolver.Resolve(resolutionContext);

            return descriptors;
        }

        // Allows MVC a chance to override the TagHelperDescriptorResolutionContext
        protected virtual TagHelperDescriptorResolutionContext GetTagHelperDescriptorResolutionContext(
            IEnumerable<TagHelperDirectiveDescriptor> descriptors,
            ErrorSink errorSink)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            if (errorSink == null)
            {
                throw new ArgumentNullException(nameof(errorSink));
            }

            return new TagHelperDescriptorResolutionContext(descriptors, errorSink);
        }

        public override void VisitSpan(Span span)
        {
            // We're only interested in spans with an AddOrRemoveTagHelperChunkGenerator.

            if (span.ChunkGenerator is AddOrRemoveTagHelperChunkGenerator)
            {
                var chunkGenerator = (AddOrRemoveTagHelperChunkGenerator)span.ChunkGenerator;

                var directive =
                    chunkGenerator.RemoveTagHelperDescriptors ?
                    TagHelperDirectiveType.RemoveTagHelper :
                    TagHelperDirectiveType.AddTagHelper;
                var textLocation = GetSubTextSourceLocation(span, chunkGenerator.LookupText);

                var directiveDescriptor = new TagHelperDirectiveDescriptor
                {
                    DirectiveText = chunkGenerator.LookupText,
                    Location = textLocation,
                    DirectiveType = directive
                };

                _directiveDescriptors.Add(directiveDescriptor);
            }
            else if (span.ChunkGenerator is TagHelperPrefixDirectiveChunkGenerator)
            {
                var chunkGenerator = (TagHelperPrefixDirectiveChunkGenerator)span.ChunkGenerator;
                var textLocation = GetSubTextSourceLocation(span, chunkGenerator.Prefix);

                var directiveDescriptor = new TagHelperDirectiveDescriptor
                {
                    DirectiveText = chunkGenerator.Prefix,
                    Location = textLocation,
                    DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                };

                _directiveDescriptors.Add(directiveDescriptor);
            }
        }

        private static SourceLocation GetSubTextSourceLocation(Span span, string text)
        {
            var startOffset = span.Content.IndexOf(text);
            var offsetContent = span.Content.Substring(0, startOffset);
            var offsetTextLocation = SourceLocation.Advance(span.Start, offsetContent);

            return offsetTextLocation;
        }
    }
}