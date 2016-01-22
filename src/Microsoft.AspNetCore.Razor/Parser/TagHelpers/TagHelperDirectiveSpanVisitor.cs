// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

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
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            TagHelperDirectiveType directiveType;
            if (span.ChunkGenerator is AddTagHelperChunkGenerator)
            {
                directiveType = TagHelperDirectiveType.AddTagHelper;
            }
            else if (span.ChunkGenerator is RemoveTagHelperChunkGenerator)
            {
                directiveType = TagHelperDirectiveType.RemoveTagHelper;
            }
            else if (span.ChunkGenerator is TagHelperPrefixDirectiveChunkGenerator)
            {
                directiveType = TagHelperDirectiveType.TagHelperPrefix;
            }
            else
            {
                // Not a chunk generator that we're interested in.
                return;
            }

            var directiveText = span.Content.Trim();
            var startOffset = span.Content.IndexOf(directiveText, StringComparison.Ordinal);
            var offsetContent = span.Content.Substring(0, startOffset);
            var offsetTextLocation = SourceLocation.Advance(span.Start, offsetContent);
            var directiveDescriptor = new TagHelperDirectiveDescriptor
            {
                DirectiveText = directiveText,
                Location = offsetTextLocation,
                DirectiveType = directiveType
            };

            _directiveDescriptors.Add(directiveDescriptor);
        }
    }
}