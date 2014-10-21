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

        private List<TagHelperDescriptor> _descriptors;

        public AddOrRemoveTagHelperSpanVisitor(ITagHelperDescriptorResolver descriptorResolver)
        {
            _descriptorResolver = descriptorResolver;
        }

        public IEnumerable<TagHelperDescriptor> GetDescriptors([NotNull] Block root)
        {
            _descriptors = new List<TagHelperDescriptor>();

            // This will recurse through the syntax tree.
            VisitBlock(root);

            return _descriptors;
        }

        public override void VisitSpan(Span span)
        {
            // We're only interested in spans with an AddOrRemoveTagHelperCodeGenerator.

            if (span.CodeGenerator is AddOrRemoveTagHelperCodeGenerator)
            {
                var codeGenerator = (AddOrRemoveTagHelperCodeGenerator)span.CodeGenerator;

                if (_descriptorResolver == null)
                {
                    var directive = codeGenerator.RemoveTagHelperDescriptors ?
                                  SyntaxConstants.CSharp.RemoveTagHelperKeyword :
                                  SyntaxConstants.CSharp.AddTagHelperKeyword;

                    throw new InvalidOperationException(
                        RazorResources.FormatTagHelpers_CannotUseDirectiveWithNoTagHelperDescriptorResolver(
                            directive, typeof(ITagHelperDescriptorResolver).FullName, typeof(RazorParser).FullName));
                }

                // Look up all the descriptors associated with the "LookupText".
                var descriptors = _descriptorResolver.Resolve(codeGenerator.LookupText);

                if (codeGenerator.RemoveTagHelperDescriptors)
                {
                    var evaluatedDescriptors = 
                        new HashSet<TagHelperDescriptor>(descriptors, TagHelperDescriptorComparer.Default);

                    // We remove all found descriptors from the descriptor list to ignore the associated TagHelpers on the 
                    // Razor page.
                    _descriptors.RemoveAll(descriptor => evaluatedDescriptors.Contains(descriptor));
                }
                else
                {
                    // Add all the found descriptors to our list.
                    _descriptors.AddRange(descriptors);
                }
            }
        }
    }
}