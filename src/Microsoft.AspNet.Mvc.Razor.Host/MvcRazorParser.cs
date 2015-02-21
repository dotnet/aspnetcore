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
    /// helpers from <c>_ViewStart</c> files.
    /// </summary>
    public class MvcRazorParser : RazorParser
    {
        private readonly IEnumerable<TagHelperDirectiveDescriptor> _viewStartDirectiveDescriptors;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorParser"/>.
        /// </summary>
        /// <param name="parser">The <see cref="RazorParser"/> to copy properties from.</param>
        /// <param name="inheritedCodeTrees">The <see cref="IReadOnlyList{CodeTree}"/>s that are inherited
        /// from parsed pages from _ViewStart files.</param>
        /// <param name="defaultInheritedChunks">The <see cref="IReadOnlyList{Chunk}"/> inherited by
        /// default by all Razor pages in the application.</param>
        public MvcRazorParser([NotNull] RazorParser parser, 
                              [NotNull] IReadOnlyList<CodeTree> inheritedCodeTrees, 
                              [NotNull] IReadOnlyList<Chunk> defaultInheritedChunks)
            : base(parser)
        {
            // Construct tag helper descriptors from @addTagHelper and @removeTagHelper chunks
            _viewStartDirectiveDescriptors = GetTagHelperDescriptors(inheritedCodeTrees, defaultInheritedChunks);
        }

        /// <inheritdoc />
        protected override IEnumerable<TagHelperDescriptor> GetTagHelperDescriptors(
            [NotNull] Block documentRoot,
            [NotNull] ParserErrorSink errorSink)
        {
            var visitor = new ViewStartAddRemoveTagHelperVisitor(TagHelperDescriptorResolver,
                                                                 _viewStartDirectiveDescriptors,
                                                                 errorSink);
            return visitor.GetDescriptors(documentRoot);
        }

        private static IEnumerable<TagHelperDirectiveDescriptor> GetTagHelperDescriptors(
           IReadOnlyList<CodeTree> inheritedCodeTrees,
           IReadOnlyList<Chunk> defaultInheritedChunks)
        {
            var descriptors = new List<TagHelperDirectiveDescriptor>();

            // For tag helpers, the @removeTagHelper only applies tag helpers that were added prior to it.
            // Consequently we must visit tag helpers outside-in - furthest _ViewStart first and nearest one last. This
            // is different from the behavior of chunk merging where we visit the nearest one first and ignore chunks
            // that were previously visited.
            var chunksFromViewStarts = inheritedCodeTrees.Reverse()
                                                         .SelectMany(tree => tree.Chunks);
            var chunksInOrder = defaultInheritedChunks.Concat(chunksFromViewStarts);
            foreach (var chunk in chunksInOrder)
            {
                var addHelperChunk = chunk as AddTagHelperChunk;
                if (addHelperChunk != null)
                {
                    var descriptor = new TagHelperDirectiveDescriptor(addHelperChunk.LookupText,
                                                                      SourceLocation.Undefined,
                                                                      TagHelperDirectiveType.AddTagHelper);
                    descriptors.Add(descriptor);
                }
                else
                {
                    var removeHelperChunk = chunk as RemoveTagHelperChunk;
                    if (removeHelperChunk != null)
                    {
                        var descriptor = new TagHelperDirectiveDescriptor(removeHelperChunk.LookupText,
                                                                          SourceLocation.Undefined,
                                                                          TagHelperDirectiveType.RemoveTagHelper);
                        descriptors.Add(descriptor);
                    }
                }
            }

            return descriptors;
        }

        private class ViewStartAddRemoveTagHelperVisitor : AddOrRemoveTagHelperSpanVisitor
        {
            private readonly IEnumerable<TagHelperDirectiveDescriptor> _viewStartDirectiveDescriptors;

            public ViewStartAddRemoveTagHelperVisitor(
                ITagHelperDescriptorResolver descriptorResolver,
                IEnumerable<TagHelperDirectiveDescriptor> viewStartDirectiveDescriptors,
                ParserErrorSink errorSink)
                : base(descriptorResolver, errorSink)
            {
                _viewStartDirectiveDescriptors = viewStartDirectiveDescriptors;
            }

            protected override TagHelperDescriptorResolutionContext GetTagHelperDescriptorResolutionContext(
                IEnumerable<TagHelperDirectiveDescriptor> descriptors,
                ParserErrorSink errorSink)
            {
                return base.GetTagHelperDescriptorResolutionContext(
                    _viewStartDirectiveDescriptors.Concat(descriptors),
                    errorSink);
            }
        }
    }
}