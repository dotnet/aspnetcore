// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

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
            [NotNull] ITagHelperDescriptorResolver descriptorResolver,
            [NotNull] ErrorSink errorSink)
        {
            _descriptorResolver = descriptorResolver;
            _errorSink = errorSink;
        }

        public IEnumerable<TagHelperDescriptor> GetDescriptors([NotNull] Block root)
        {
            _directiveDescriptors = new List<TagHelperDirectiveDescriptor>();

            // This will recurse through the syntax tree.
            VisitBlock(root);

            var resolutionContext = GetTagHelperDescriptorResolutionContext(_directiveDescriptors, _errorSink);
            var descriptors = _descriptorResolver.Resolve(resolutionContext);

            return descriptors;
        }

        // Allows MVC a chance to override the TagHelperDescriptorResolutionContext
        protected virtual TagHelperDescriptorResolutionContext GetTagHelperDescriptorResolutionContext(
            [NotNull] IEnumerable<TagHelperDirectiveDescriptor> descriptors,
            [NotNull] ErrorSink errorSink)
        {
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

                var directiveDescriptor = new TagHelperDirectiveDescriptor
                {
                    DirectiveText = chunkGenerator.LookupText,
                    Location = span.Start,
                    DirectiveType = directive
                };

                _directiveDescriptors.Add(directiveDescriptor);
            }
            else if (span.ChunkGenerator is TagHelperPrefixDirectiveChunkGenerator)
            {
                var chunkGenerator = (TagHelperPrefixDirectiveChunkGenerator)span.ChunkGenerator;

                var directiveDescriptor = new TagHelperDirectiveDescriptor
                {
                    DirectiveText = chunkGenerator.Prefix,
                    Location = span.Start,
                    DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                };

                _directiveDescriptors.Add(directiveDescriptor);
            }
        }
    }
}