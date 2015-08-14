// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.TagHelpers.Internal;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;

namespace Microsoft.AspNet.Razor.Test.TagHelpers
{
    public class TagHelperRewritingTestBase : CsHtmlMarkupParserTestBase
    {
        public void RunParseTreeRewriterTest(
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

        public void RunParseTreeRewriterTest(
            string documentContent,
            MarkupBlock expectedOutput,
            IEnumerable<RazorError> errors,
            params string[] tagNames)
        {
            var providerContext = BuildProviderContext(tagNames);

            EvaluateData(providerContext, documentContent, expectedOutput, errors);
        }

        public TagHelperDescriptorProvider BuildProviderContext(params string[] tagNames)
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

        public override ParserContext CreateParserContext(
            ITextDocument input,
            ParserBase codeParser,
            ParserBase markupParser,
            ErrorSink errorSink)
        {
            return base.CreateParserContext(input, codeParser, markupParser, errorSink);
        }

        public void EvaluateData(
            TagHelperDescriptorProvider provider,
            string documentContent,
            MarkupBlock expectedOutput,
            IEnumerable<RazorError> expectedErrors)
        {
            var errorSink = new ErrorSink();
            var results = ParseDocument(documentContent, errorSink);
            var rewritingContext = new RewritingContext(results.Document, errorSink);
            new TagHelperParseTreeRewriter(provider).Rewrite(rewritingContext);
            var rewritten = rewritingContext.SyntaxTree;
            var actualErrors = errorSink.Errors.OrderBy(error => error.Location.AbsoluteIndex)
                                               .ToList();

            EvaluateRazorErrors(actualErrors, expectedErrors.ToList());
            EvaluateParseTree(rewritten, expectedOutput);
        }

        public static SpanFactory CreateDefaultSpanFactory()
        {
            return new SpanFactory
            {
                MarkupTokenizerFactory = doc => new HtmlTokenizer(doc),
                CodeTokenizerFactory = doc => new CSharpTokenizer(doc)
            };
        }
    }
}
