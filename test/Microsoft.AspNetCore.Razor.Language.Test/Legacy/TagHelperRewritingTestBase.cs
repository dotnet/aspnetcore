// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TagHelperRewritingTestBase : CsHtmlMarkupParserTestBase
    {
        internal void RunParseTreeRewriterTest(
            string documentContent,
            MarkupBlock expectedOutput,
            params string[] tagNames)
        {
            RunParseTreeRewriterTest(
                documentContent,
                expectedOutput,
                errors: Enumerable.Empty<RazorError>(),
                tagNames: tagNames);
        }

        internal void RunParseTreeRewriterTest(
            string documentContent,
            MarkupBlock expectedOutput,
            IEnumerable<RazorError> errors,
            params string[] tagNames)
        {
            var providerContext = BuildProviderContext(tagNames);

            EvaluateData(providerContext, documentContent, expectedOutput, errors);
        }

        internal TagHelperDescriptorProvider BuildProviderContext(params string[] tagNames)
        {
            var descriptors = new List<TagHelperDescriptor>();

            foreach (var tagName in tagNames)
            {
                var descriptor = TagHelperDescriptorBuilder.Create(tagName + "taghelper", "SomeAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName(tagName))
                    .Build();
                descriptors.Add(descriptor);
            }

            return new TagHelperDescriptorProvider(null, descriptors);
        }

        internal void EvaluateData(
            TagHelperDescriptorProvider provider,
            string documentContent,
            MarkupBlock expectedOutput,
            IEnumerable<RazorError> expectedErrors,
            string tagHelperPrefix = null)
        {
            var syntaxTree = ParseDocument(documentContent);
            var errorSink = new ErrorSink();
            var parseTreeRewriter = new TagHelperParseTreeRewriter(tagHelperPrefix, provider);
            var actualTree = parseTreeRewriter.Rewrite(syntaxTree.Root, errorSink);

            var allErrors = syntaxTree.Diagnostics.Concat(errorSink.Errors.Select(error => RazorDiagnostic.Create(error)));
            var actualErrors = allErrors
                .OrderBy(error => error.Span.AbsoluteIndex)
                .ToList();

            EvaluateRazorErrors(actualErrors, expectedErrors.Select(error => RazorDiagnostic.Create(error)).ToList());
            EvaluateParseTree(actualTree, expectedOutput);
        }
    }
}
