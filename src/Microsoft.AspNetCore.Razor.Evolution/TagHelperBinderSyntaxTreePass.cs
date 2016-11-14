// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class TagHelperBinderSyntaxTreePass : IRazorSyntaxTreePass
    {
        public RazorEngine Engine { get; set; }

        public int Order => 100;

        public RazorSyntaxTree Execute(RazorCodeDocument document, RazorSyntaxTree syntaxTree)
        {
            var resolver = Engine.Features.OfType<TagHelperFeature>().FirstOrDefault()?.Resolver;
            if (resolver == null)
            {
                // No resolver, nothing to do.
                return syntaxTree;
            }

            var errorSink = new ErrorSink();
            var visitor = new TagHelperDirectiveSpanVisitor(resolver, errorSink);
            var descriptors = visitor.GetDescriptors(syntaxTree.Root);

            if (!descriptors.Any())
            {
                if (errorSink.Errors.Count > 0)
                {
                    var combinedErrors = CombineErrors(syntaxTree.Diagnostics, errorSink.Errors);
                    var erroredTree = RazorSyntaxTree.Create(syntaxTree.Root, combinedErrors);

                    return erroredTree;
                }

                return syntaxTree;
            }

            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);
            var rewriter = new TagHelperParseTreeRewriter(descriptorProvider);
            var rewrittenRoot = rewriter.Rewrite(syntaxTree.Root, errorSink);
            var diagnostics = syntaxTree.Diagnostics;

            if (errorSink.Errors.Count > 0)
            {
                diagnostics = CombineErrors(diagnostics, errorSink.Errors);
            }

            var newSyntaxTree = RazorSyntaxTree.Create(rewrittenRoot, diagnostics);
            return newSyntaxTree;
        }

        private IReadOnlyList<RazorError> CombineErrors(IReadOnlyList<RazorError> errors1, IReadOnlyList<RazorError> errors2)
        {
            var combinedErrors = new List<RazorError>(errors1.Count + errors2.Count);
            combinedErrors.AddRange(errors1);
            combinedErrors.AddRange(errors2);

            return combinedErrors;
        }
    }
}
