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
            var descriptors = BuildDescriptors(tagNames);

            EvaluateData(descriptors, documentContent, expectedOutput, errors);
        }

        internal IEnumerable<TagHelperDescriptor> BuildDescriptors(params string[] tagNames)
        {
            var descriptors = new List<TagHelperDescriptor>();

            foreach (var tagName in tagNames)
            {
                var descriptor = TagHelperDescriptorBuilder.Create(tagName + "taghelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName(tagName))
                    .Build();
                descriptors.Add(descriptor);
            }

            return descriptors;
        }

        internal void EvaluateData(
            IEnumerable<TagHelperDescriptor> descriptors,
            string documentContent,
            MarkupBlock expectedOutput,
            IEnumerable<RazorError> expectedErrors,
            string tagHelperPrefix = null,
            RazorParserFeatureFlags featureFlags = null)
        {
            var syntaxTree = ParseDocument(documentContent);
            var errorSink = new ErrorSink();
            var parseTreeRewriter = new TagHelperParseTreeRewriter(
                tagHelperPrefix,
                descriptors,
                featureFlags ?? syntaxTree.Options.FeatureFlags);

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
