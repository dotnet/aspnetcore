// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// A subtype of <see cref="RazorParser"/> that <see cref="MvcRazorHost"/> uses to support inheritance of tag
    /// helpers from <c>_GlobalImport</c> files.
    /// </summary>
    public class MvcRazorParser : RazorParser
    {
        private readonly IEnumerable<TagHelperDirectiveDescriptor> _globalImportDirectiveDescriptors;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorParser"/>.
        /// </summary>
        /// <param name="parser">The <see cref="RazorParser"/> to copy properties from.</param>
        /// <param name="inheritedCodeTrees">The <see cref="IReadOnlyList{CodeTree}"/>s that are inherited
        /// from parsed pages from _GlobalImport files.</param>
        /// <param name="defaultInheritedChunks">The <see cref="IReadOnlyList{Chunk}"/> inherited by
        /// default by all Razor pages in the application.</param>
        public MvcRazorParser(
            [NotNull] RazorParser parser,
            [NotNull] IReadOnlyList<CodeTree> inheritedCodeTrees,
            [NotNull] IReadOnlyList<Chunk> defaultInheritedChunks)
            : base(parser)
        {
            // Construct tag helper descriptors from @addTagHelper, @removeTagHelper and @tagHelperPrefix chunks
            _globalImportDirectiveDescriptors = GetTagHelperDirectiveDescriptors(
                inheritedCodeTrees,
                defaultInheritedChunks);
        }

        /// <inheritdoc />
        protected override IEnumerable<TagHelperDescriptor> GetTagHelperDescriptors(
            [NotNull] Block documentRoot,
            [NotNull] ParserErrorSink errorSink)
        {
            var visitor = new GlobalImportTagHelperDirectiveSpanVisitor(
                TagHelperDescriptorResolver,
                _globalImportDirectiveDescriptors,
                errorSink);
            return visitor.GetDescriptors(documentRoot);
        }

        private static IEnumerable<TagHelperDirectiveDescriptor> GetTagHelperDirectiveDescriptors(
           IReadOnlyList<CodeTree> inheritedCodeTrees,
           IReadOnlyList<Chunk> defaultInheritedChunks)
        {
            var descriptors = new List<TagHelperDirectiveDescriptor>();

            // For tag helpers, the @removeTagHelper only applies tag helpers that were added prior to it.
            // Consequently we must visit tag helpers outside-in - furthest _GlobalImport first and nearest one last.
            // This is different from the behavior of chunk merging where we visit the nearest one first and ignore
            // chunks that were previously visited.
            var chunksFromGlobalImports = inheritedCodeTrees
                .Reverse()
                .SelectMany(tree => tree.Chunks);
            var chunksInOrder = defaultInheritedChunks.Concat(chunksFromGlobalImports);
            foreach (var chunk in chunksInOrder)
            {
                // All TagHelperDirectiveDescriptors created here have undefined source locations because the source 
                // that created them is not in the same file.
                var addTagHelperChunk = chunk as AddTagHelperChunk;
                if (addTagHelperChunk != null)
                {
                    var descriptor = new TagHelperDirectiveDescriptor(
                        addTagHelperChunk.LookupText,
                        SourceLocation.Undefined,
                        TagHelperDirectiveType.AddTagHelper);

                    descriptors.Add(descriptor);

                    continue;
                }

                var removeTagHelperChunk = chunk as RemoveTagHelperChunk;
                if (removeTagHelperChunk != null)
                {
                    var descriptor = new TagHelperDirectiveDescriptor(
                        removeTagHelperChunk.LookupText,
                        SourceLocation.Undefined,
                        TagHelperDirectiveType.RemoveTagHelper);

                    descriptors.Add(descriptor);

                    continue;
                }

                var tagHelperPrefixDirectiveChunk = chunk as TagHelperPrefixDirectiveChunk;
                if (tagHelperPrefixDirectiveChunk != null)
                {
                    var descriptor = new TagHelperDirectiveDescriptor(
                        tagHelperPrefixDirectiveChunk.Prefix,
                        SourceLocation.Undefined,
                        TagHelperDirectiveType.TagHelperPrefix);

                    descriptors.Add(descriptor);
                }
            }

            return descriptors;
        }

        private class GlobalImportTagHelperDirectiveSpanVisitor : TagHelperDirectiveSpanVisitor
        {
            private readonly IEnumerable<TagHelperDirectiveDescriptor> _globalImportDirectiveDescriptors;

            public GlobalImportTagHelperDirectiveSpanVisitor(
                ITagHelperDescriptorResolver descriptorResolver,
                IEnumerable<TagHelperDirectiveDescriptor> globalImportDirectiveDescriptors,
                ParserErrorSink errorSink)
                : base(descriptorResolver, errorSink)
            {
                _globalImportDirectiveDescriptors = globalImportDirectiveDescriptors;
            }

            protected override TagHelperDescriptorResolutionContext GetTagHelperDescriptorResolutionContext(
                IEnumerable<TagHelperDirectiveDescriptor> descriptors,
                ParserErrorSink errorSink)
            {
                var directivesToImport = MergeDirectiveDescriptors(descriptors, _globalImportDirectiveDescriptors);

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