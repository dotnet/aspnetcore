// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Parser.TagHelpers
{
    /// <summary>
    /// A <see cref="ParserVisitor"/> that generates <see cref="TagHelperDescriptor"/>s from
    /// tag helper code generators in a Razor document.
    /// </summary>
    public class AddOrRemoveTagHelperSpanVisitor : ParserVisitor
    {
        private readonly ITagHelperDescriptorResolver _descriptorResolver;

        private List<TagHelperDirectiveDescriptor> _directiveDescriptors;

        public AddOrRemoveTagHelperSpanVisitor([NotNull] ITagHelperDescriptorResolver descriptorResolver)
        {
            _descriptorResolver = descriptorResolver;
        }

        public IEnumerable<TagHelperDescriptor> GetDescriptors([NotNull] Block root)
        {
            _directiveDescriptors = new List<TagHelperDirectiveDescriptor>();

            // This will recurse through the syntax tree.
            VisitBlock(root);

            var resolutionContext = new TagHelperDescriptorResolutionContext(_directiveDescriptors);
            var descriptors = _descriptorResolver.Resolve(resolutionContext);

            return descriptors;
        }

        public override void VisitSpan(Span span)
        {
            // We're only interested in spans with an AddOrRemoveTagHelperCodeGenerator.

            if (span.CodeGenerator is AddOrRemoveTagHelperCodeGenerator)
            {
                var codeGenerator = (AddOrRemoveTagHelperCodeGenerator)span.CodeGenerator;

                var directive = codeGenerator.RemoveTagHelperDescriptors ?
                                TagHelperDirectiveType.RemoveTagHelper :
                                TagHelperDirectiveType.AddTagHelper;

                var directiveDescriptor = new TagHelperDirectiveDescriptor(codeGenerator.LookupText, directive);

                _directiveDescriptors.Add(directiveDescriptor);
            }
        }
    }
}