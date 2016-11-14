// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class TagHelperDirectiveSpanVisitor
    {
        private readonly ITagHelperDescriptorResolver _descriptorResolver;
        private readonly ErrorSink _errorSink;

        private List<TagHelperDirectiveDescriptor> _directiveDescriptors;

        public int Order { get; }

        public RazorEngine Engine { get; set; }

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

        public void VisitBlock(Block block)
        {
            for (var i = 0; i < block.Children.Count; i++)
            {
                var child = block.Children[i];

                if (child.IsBlock)
                {
                    VisitBlock((Block)child);
                }
                else
                {
                    VisitSpan((Span)child);
                }
            }
        }

        public void VisitSpan(Span span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            string directiveText;
            TagHelperDirectiveType directiveType;

            var addTagHelperChunkGenerator = span.ChunkGenerator as AddTagHelperChunkGenerator;
            var removeTagHelperChunkGenerator = span.ChunkGenerator as RemoveTagHelperChunkGenerator;
            var tagHelperPrefixChunkGenerator = span.ChunkGenerator as TagHelperPrefixDirectiveChunkGenerator;

            if (addTagHelperChunkGenerator != null)
            {
                directiveType = TagHelperDirectiveType.AddTagHelper;
                directiveText = addTagHelperChunkGenerator.LookupText;
            }
            else if (removeTagHelperChunkGenerator != null)
            {
                directiveType = TagHelperDirectiveType.RemoveTagHelper;
                directiveText = removeTagHelperChunkGenerator.LookupText;
            }
            else if (tagHelperPrefixChunkGenerator != null)
            {
                directiveType = TagHelperDirectiveType.TagHelperPrefix;
                directiveText = tagHelperPrefixChunkGenerator.Prefix;
            }
            else
            {
                // Not a chunk generator that we're interested in.
                return;
            }

            directiveText = directiveText.Trim();
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

        public RazorSyntaxTree Execute(RazorCodeDocument codeDocument, RazorSyntaxTree syntaxTree)
        {
            throw new NotImplementedException();
        }
    }
}