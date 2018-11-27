// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TagHelperRewritingTestBase : CsHtmlMarkupParserTestBase
    {
        internal void RunParseTreeRewriterTest(string documentContent, params string[] tagNames)
        {
            var descriptors = BuildDescriptors(tagNames);

            EvaluateData(descriptors, documentContent);
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
            string tagHelperPrefix = null,
            RazorParserFeatureFlags featureFlags = null)
        {
            var syntaxTree = ParseDocument(documentContent, featureFlags: featureFlags);
            var errorSink = new ErrorSink();

            var rewrittenTree = TagHelperParseTreeRewriter.Rewrite(syntaxTree, tagHelperPrefix, descriptors);

            BaselineTest(rewrittenTree, verifySyntaxTree: false);
        }
    }
}
