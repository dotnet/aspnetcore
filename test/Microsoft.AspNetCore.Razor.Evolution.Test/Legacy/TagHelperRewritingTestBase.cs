// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Evolution.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
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
                descriptors.Add(
                    new TagHelperDescriptor
                    {
                        TagName = tagName,
                        TypeName = tagName + "taghelper",
                        AssemblyName = "SomeAssembly"
                    });
            }

            return new TagHelperDescriptorProvider(descriptors);
        }

        internal void EvaluateData(
            TagHelperDescriptorProvider provider,
            string documentContent,
            MarkupBlock expectedOutput,
            IEnumerable<RazorError> expectedErrors)
        {
            var syntaxTree = ParseDocument(documentContent);
            var errorSink = new ErrorSink();
            var parseTreeRewriter = new TagHelperParseTreeRewriter(provider);
            var actualTree = parseTreeRewriter.Rewrite(syntaxTree.Root, errorSink);

            var allErrors = syntaxTree.Diagnostics.Concat(errorSink.Errors);
            var actualErrors = allErrors
                .OrderBy(error => error.Location.AbsoluteIndex)
                .ToList();

            EvaluateRazorErrors(actualErrors, expectedErrors.ToList());
            EvaluateParseTree(actualTree, expectedOutput);
        }
    }
}
