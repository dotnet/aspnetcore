// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// A subtype of <see cref="RazorParser"/> that <see cref="MvcRazorHost"/> uses to support inheritance of tag
    /// helpers from <c>_ViewStart</c> files.
    /// </summary>
    public class MvcRazorParser : RazorParser
    {
        private readonly IReadOnlyList<Chunk> _viewStartChunks;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorParser"/>.
        /// </summary>
        /// <param name="parser">The <see cref="RazorParser"/> to copy properties from.</param>
        /// <param name="viewStartChunks">The <see cref="IReadOnlyList{T}"/> of <see cref="Chunk"/>s that are inherited
        /// by parsed pages from _ViewStart files.</param>
        public MvcRazorParser(RazorParser parser, IReadOnlyList<Chunk> viewStartChunks)
            : base(parser)
        {
            _viewStartChunks = viewStartChunks;
        }

        /// <inheritdoc />
        protected override IEnumerable<TagHelperDescriptor> GetTagHelperDescriptors(
            [NotNull] Block documentRoot,
            [NotNull] ParserErrorSink errorSink)
        {
            // Grab all the @addtaghelper chunks from view starts and construct TagHelperDirectiveDescriptors
            var directiveDescriptors = _viewStartChunks.OfType<AddTagHelperChunk>()
                                                       .Select(chunk => new TagHelperDirectiveDescriptor(
                                                            chunk.LookupText,
                                                            chunk.Start,
                                                            TagHelperDirectiveType.AddTagHelper));

            var visitor = new ViewStartAddRemoveTagHelperVisitor(TagHelperDescriptorResolver,
                                                                 directiveDescriptors,
                                                                 errorSink);
            var descriptors = visitor.GetDescriptors(documentRoot);

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