// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Parser.TagHelpers.Internal
{
    public class TagHelperRegistrationVisitor : ParserVisitor
    {
        private readonly ITagHelperDescriptorResolver _descriptorResolver;

        private HashSet<TagHelperDescriptor> _descriptors;

        public TagHelperRegistrationVisitor(ITagHelperDescriptorResolver descriptorResolver)
        {
            _descriptorResolver = descriptorResolver;
        }

        public TagHelperDescriptorProvider CreateProvider(Block root)
        {
            _descriptors = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);

            // This will recurse through the syntax tree.
            VisitBlock(root);

            return new TagHelperDescriptorProvider(_descriptors);
        }

        public override void VisitSpan(Span span)
        {
            // We're only interested in spans with an AddTagHelperCodeGenerator.
            if (span.CodeGenerator is AddTagHelperCodeGenerator)
            {
                if (_descriptorResolver == null)
                {
                    throw new InvalidOperationException(
                        RazorResources.FormatTagHelpers_CannotUseDirectiveWithNoTagHelperDescriptorResolver(
                            SyntaxConstants.CSharp.AddTagHelperKeyword,
                            nameof(TagHelperDescriptorResolver),
                            nameof(RazorParser)));
                }

                var addGenerator = (AddTagHelperCodeGenerator)span.CodeGenerator;

                // Look up all the descriptors associated with the "LookupText".
                var descriptors = _descriptorResolver.Resolve(addGenerator.LookupText);

                // Add all the found descriptors to our HashSet.
                foreach (var descriptor in descriptors)
                {
                    _descriptors.Add(descriptor);
                }
            }
        }
    }
}