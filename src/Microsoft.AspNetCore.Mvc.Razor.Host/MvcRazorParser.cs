// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.Host;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Parser.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// A subtype of <see cref="RazorParser"/> that <see cref="MvcRazorHost"/> uses to support inheritance of tag
    /// helpers from <c>_ViewImports</c> files.
    /// </summary>
    public class MvcRazorParser : RazorParser
    {
        private readonly IEnumerable<TagHelperDirectiveDescriptor> _viewImportsDirectiveDescriptors;
        private readonly string _modelExpressionTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorParser"/>.
        /// </summary>
        /// <param name="parser">The <see cref="RazorParser"/> to copy properties from.</param>
        /// <param name="inheritedChunkTrees">The <see cref="IReadOnlyList{ChunkTree}"/>s that are inherited
        /// from parsed pages from _ViewImports files.</param>
        /// <param name="defaultInheritedChunks">The <see cref="IReadOnlyList{Chunk}"/> inherited by
        /// default by all Razor pages in the application.</param>
        /// <param name="modelExpressionTypeName">The full name of the model expression <see cref="Type"/>.</param>
        public MvcRazorParser(
            RazorParser parser,
            IReadOnlyList<ChunkTree> inheritedChunkTrees,
            IReadOnlyList<Chunk> defaultInheritedChunks,
            string modelExpressionTypeName)
            : base(parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            if (inheritedChunkTrees == null)
            {
                throw new ArgumentNullException(nameof(inheritedChunkTrees));
            }

            if (defaultInheritedChunks == null)
            {
                throw new ArgumentNullException(nameof(defaultInheritedChunks));
            }

            if (modelExpressionTypeName == null)
            {
                throw new ArgumentNullException(nameof(modelExpressionTypeName));
            }

            // Construct tag helper descriptors from @addTagHelper, @removeTagHelper and @tagHelperPrefix chunks
            _viewImportsDirectiveDescriptors = GetTagHelperDirectiveDescriptors(
                inheritedChunkTrees,
                defaultInheritedChunks);

            _modelExpressionTypeName = modelExpressionTypeName;
        }

        /// <inheritdoc />
        protected override IEnumerable<TagHelperDescriptor> GetTagHelperDescriptors(
            Block documentRoot,
            ErrorSink errorSink)
        {
            if (documentRoot == null)
            {
                throw new ArgumentNullException(nameof(documentRoot));
            }

            if (errorSink == null)
            {
                throw new ArgumentNullException(nameof(errorSink));
            }

            var visitor = new ViewImportsTagHelperDirectiveSpanVisitor(
                TagHelperDescriptorResolver,
                _viewImportsDirectiveDescriptors,
                errorSink);

            var descriptors = visitor.GetDescriptors(documentRoot);
            foreach (var descriptor in descriptors)
            {
                foreach (var attributeDescriptor in descriptor.Attributes)
                {
                    if (attributeDescriptor.IsIndexer &&
                        string.Equals(
                            attributeDescriptor.TypeName,
                            _modelExpressionTypeName,
                            StringComparison.Ordinal))
                    {
                        errorSink.OnError(
                            SourceLocation.Undefined,
                            Resources.FormatMvcRazorParser_InvalidPropertyType(
                                descriptor.TypeName,
                                attributeDescriptor.Name,
                                _modelExpressionTypeName),
                            length: 0);
                    }
                }
            }

            return descriptors;
        }

        private static IEnumerable<TagHelperDirectiveDescriptor> GetTagHelperDirectiveDescriptors(
           IReadOnlyList<ChunkTree> inheritedChunkTrees,
           IReadOnlyList<Chunk> defaultInheritedChunks)
        {
            var descriptors = new List<TagHelperDirectiveDescriptor>();

            var inheritedChunks = defaultInheritedChunks.Concat(inheritedChunkTrees.SelectMany(tree => tree.Children));
            foreach (var chunk in inheritedChunks)
            {
                // All TagHelperDirectiveDescriptors created here have undefined source locations because the source
                // that created them is not in the same file.
                var addTagHelperChunk = chunk as AddTagHelperChunk;
                if (addTagHelperChunk != null)
                {
                    var descriptor = new TagHelperDirectiveDescriptor
                    {
                        DirectiveText = addTagHelperChunk.LookupText,
                        Location = chunk.Start,
                        DirectiveType = TagHelperDirectiveType.AddTagHelper
                    };

                    descriptors.Add(descriptor);

                    continue;
                }

                var removeTagHelperChunk = chunk as RemoveTagHelperChunk;
                if (removeTagHelperChunk != null)
                {
                    var descriptor = new TagHelperDirectiveDescriptor
                    {
                        DirectiveText = removeTagHelperChunk.LookupText,
                        Location = chunk.Start,
                        DirectiveType = TagHelperDirectiveType.RemoveTagHelper
                    };

                    descriptors.Add(descriptor);

                    continue;
                }

                var tagHelperPrefixDirectiveChunk = chunk as TagHelperPrefixDirectiveChunk;
                if (tagHelperPrefixDirectiveChunk != null)
                {
                    var descriptor = new TagHelperDirectiveDescriptor
                    {
                        DirectiveText = tagHelperPrefixDirectiveChunk.Prefix,
                        Location = chunk.Start,
                        DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                    };

                    descriptors.Add(descriptor);
                }
            }

            return descriptors;
        }

        private class ViewImportsTagHelperDirectiveSpanVisitor : TagHelperDirectiveSpanVisitor
        {
            private readonly IEnumerable<TagHelperDirectiveDescriptor> _viewImportsDirectiveDescriptors;

            public ViewImportsTagHelperDirectiveSpanVisitor(
                ITagHelperDescriptorResolver descriptorResolver,
                IEnumerable<TagHelperDirectiveDescriptor> viewImportsDirectiveDescriptors,
                ErrorSink errorSink)
                : base(descriptorResolver, errorSink)
            {
                _viewImportsDirectiveDescriptors = viewImportsDirectiveDescriptors;
            }

            protected override TagHelperDescriptorResolutionContext GetTagHelperDescriptorResolutionContext(
                IEnumerable<TagHelperDirectiveDescriptor> descriptors,
                ErrorSink errorSink)
            {
                var directivesToImport = MergeDirectiveDescriptors(descriptors, _viewImportsDirectiveDescriptors);

                return base.GetTagHelperDescriptorResolutionContext(directivesToImport, errorSink);
            }

            private static IEnumerable<TagHelperDirectiveDescriptor> MergeDirectiveDescriptors(
                IEnumerable<TagHelperDirectiveDescriptor> descriptors,
                IEnumerable<TagHelperDirectiveDescriptor> inheritedDescriptors)
            {
                var mergedDescriptors = new List<TagHelperDirectiveDescriptor>();
                TagHelperDirectiveDescriptor prefixDirectiveDescriptor = null;

                foreach (var descriptor in inheritedDescriptors)
                {
                    if (descriptor.DirectiveType == TagHelperDirectiveType.TagHelperPrefix)
                    {
                        // Always take the latest @tagHelperPrefix descriptor. Can only have 1 per page.
                        prefixDirectiveDescriptor = descriptor;
                    }
                    else
                    {
                        mergedDescriptors.Add(descriptor);
                    }
                }

                // We need to see if the provided descriptors contain a @tagHelperPrefix directive. If so, it
                // takes precedence and overrides any provided by the inheritedDescriptors. If not we need to add the
                // inherited @tagHelperPrefix directive back into the merged list.
                if (prefixDirectiveDescriptor != null &&
                    !descriptors.Any(descriptor => descriptor.DirectiveType == TagHelperDirectiveType.TagHelperPrefix))
                {
                    mergedDescriptors.Add(prefixDirectiveDescriptor);
                }

                mergedDescriptors.AddRange(descriptors);

                return mergedDescriptors;
            }
        }
    }
}