// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class PaddingTest
    {
        [Fact]
        public void CalculatePaddingForEmptySpanReturnsZero()
        {
            RazorEngineHost host = CreateHost(designTime: true);

            Span span = new Span(new SpanBuilder());

            int padding = CodeGeneratorPaddingHelper.CalculatePadding(host, span, 0);

            Assert.Equal(0, padding);
        }

        [Theory]
        [InlineData(true, false, 4)]
        [InlineData(true, false, 2)]
        [InlineData(false, true, 4)]
        [InlineData(false, true, 2)]
        [InlineData(false, false, 4)]
        [InlineData(false, false, 2)]
        [InlineData(true, true, 4)]
        [InlineData(true, true, 2)]
        [InlineData(true, true, 1)]
        [InlineData(true, true, 0)]
        public void CalculatePaddingForEmptySpanWith4Spaces(bool designTime, bool isIndentingWithTabs, int tabSize)
        {
            RazorEngineHost host = CreateHost(designTime: designTime, isIndentingWithTabs: isIndentingWithTabs, tabSize: tabSize);

            Span span = GenerateSpan(@"    @{", SpanKind.Code, 3, "");

            int padding = CodeGeneratorPaddingHelper.CalculatePadding(host, span, 0);

            Assert.Equal(6, padding);
        }

        [Theory]
        [InlineData(true, false, 4)]
        [InlineData(true, false, 2)]
        [InlineData(false, true, 4)]
        [InlineData(false, true, 2)]
        [InlineData(false, false, 4)]
        [InlineData(false, false, 2)]
        [InlineData(true, true, 4)]
        [InlineData(true, true, 2)]
        [InlineData(true, true, 1)]
        [InlineData(true, true, 0)]
        public void CalculatePaddingForIfSpanWith4Spaces(bool designTime, bool isIndentingWithTabs, int tabSize)
        {
            RazorEngineHost host = CreateHost(designTime: designTime, isIndentingWithTabs: isIndentingWithTabs, tabSize: tabSize);

            Span span = GenerateSpan(@"    @if (true)", SpanKind.Code, 2, "if (true)");

            int padding = CodeGeneratorPaddingHelper.CalculatePadding(host, span, 1);

            Assert.Equal(4, padding);
        }

        [Theory]
        [InlineData(true, false, 4, 0, 4)]
        [InlineData(true, false, 2, 0, 4)]
        [InlineData(true, true, 4, 1, 0)]
        [InlineData(true, true, 2, 2, 0)]
        [InlineData(true, true, 1, 4, 0)]
        [InlineData(true, true, 0, 4, 0)]
        [InlineData(true, true, 3, 1, 1)]

        // in non design time mode padding falls back to spaces to keep runtime code identical to v2 code.
        [InlineData(false, true, 4, 0, 5)]
        [InlineData(false, true, 2, 0, 5)]

        [InlineData(false, false, 4, 0, 5)]
        [InlineData(false, false, 2, 0, 5)]
        public void VerifyPaddingForIfSpanWith4Spaces(bool designTime, bool isIndentingWithTabs, int tabSize, int numTabs, int numSpaces)
        {
            RazorEngineHost host = CreateHost(designTime: designTime, isIndentingWithTabs: isIndentingWithTabs, tabSize: tabSize);

            // no new lines involved
            Span span = GenerateSpan("    @if (true)", SpanKind.Code, 2, "if (true)");

            int generatedStart = 1;
            string code = " if (true)";
            int paddingCharCount;

            string padded = CodeGeneratorPaddingHelper.PadStatement(host, code, span, ref generatedStart, out paddingCharCount);

            VerifyPadded(numTabs, numSpaces, code, padded, paddingCharCount);

            // with new lines involved
            Span newLineSpan = GenerateSpan("\t<div>\r\n    @if (true)", SpanKind.Code, 3, "if (true)");

            string newLinePadded = CodeGeneratorPaddingHelper.PadStatement(host, code, span, ref generatedStart, out paddingCharCount);

            VerifyPadded(numTabs, numSpaces, code, newLinePadded, paddingCharCount);
        }

        [Theory]
        [InlineData(true, false, 4, 0, 8)]
        [InlineData(true, false, 2, 0, 4)]
        [InlineData(true, true, 4, 2, 0)]
        [InlineData(true, true, 2, 2, 0)]
        [InlineData(true, true, 1, 2, 0)]
        [InlineData(true, true, 0, 2, 0)]
        [InlineData(true, true, 3, 2, 0)]

        // in non design time mode padding falls back to spaces to keep runtime code identical to v2 code.
        [InlineData(false, true, 4, 0, 9)]
        [InlineData(false, true, 2, 0, 5)]

        [InlineData(false, false, 4, 0, 9)]
        [InlineData(false, false, 2, 0, 5)]
        public void VerifyPaddingForIfSpanWithTwoTabs(bool designTime, bool isIndentingWithTabs, int tabSize, int numTabs, int numSpaces)
        {
            RazorEngineHost host = CreateHost(designTime: designTime, isIndentingWithTabs: isIndentingWithTabs, tabSize: tabSize);

            // no new lines involved
            Span span = GenerateSpan("\t\t@if (true)", SpanKind.Code, 2, "if (true)");

            int generatedStart = 1;
            string code = " if (true)";
            int paddingCharCount;

            string padded = CodeGeneratorPaddingHelper.PadStatement(host, code, span, ref generatedStart, out paddingCharCount);

            VerifyPadded(numTabs, numSpaces, code, padded, paddingCharCount);

            // with new lines involved
            Span newLineSpan = GenerateSpan("\t<div>\r\n\t\t@if (true)", SpanKind.Code, 3, "if (true)");

            string newLinePadded = CodeGeneratorPaddingHelper.PadStatement(host, code, span, ref generatedStart, out paddingCharCount);

            VerifyPadded(numTabs, numSpaces, code, newLinePadded, paddingCharCount);
        }

        [Theory]
        [InlineData(true, false, 4, 0, 8)]
        [InlineData(true, false, 2, 0, 4)]
        [InlineData(true, true, 4, 2, 0)]
        [InlineData(true, true, 2, 2, 0)]
        [InlineData(true, true, 1, 2, 0)]
        [InlineData(true, true, 0, 2, 0)]

        // in non design time mode padding falls back to spaces to keep runtime code identical to v2 code.
        [InlineData(false, true, 4, 0, 9)]
        [InlineData(false, true, 2, 0, 5)]

        [InlineData(false, false, 4, 0, 9)]
        [InlineData(false, false, 2, 0, 5)]
        public void CalculatePaddingForOpenedIf(bool designTime, bool isIndentingWithTabs, int tabSize, int numTabs, int numSpaces)
        {
            RazorEngineHost host = CreateHost(designTime: designTime, isIndentingWithTabs: isIndentingWithTabs, tabSize: tabSize);

            string text = "\r\n<html>\r\n<body>\r\n\t\t@if (true) { \r\n</body>\r\n</html>";

            Span span = GenerateSpan(text, SpanKind.Code, 3, "if (true) { \r\n");

            int generatedStart = 1;
            string code = " if (true) { \r\n";
            int paddingCharCount;
            string padded = CodeGeneratorPaddingHelper.PadStatement(host, code, span, ref generatedStart, out paddingCharCount);

            VerifyPadded(numTabs, numSpaces, code, padded, paddingCharCount);
        }

        private static void VerifyPadded(int numTabs, int numSpaces, string code, string padded, int paddingCharCount)
        {
            Assert.Equal(numTabs + numSpaces + code.Length, padded.Length);

            if (numTabs > 0 || numSpaces > 0)
            {
                Assert.True(padded.Length > numTabs + numSpaces, "padded string too short");
            }

            for (int i = 0; i < numTabs; i++)
            {
                Assert.Equal('\t', padded[i]);
            }

            for (int i = numTabs; i < numTabs + numSpaces; i++)
            {
                Assert.Equal(' ', padded[i]);
            }

            Assert.Equal(numSpaces + numTabs, paddingCharCount);
        }

        private static RazorEngineHost CreateHost(bool designTime, bool isIndentingWithTabs = false, int tabSize = 4)
        {
            return new RazorEngineHost(new CSharpRazorCodeLanguage())
            {
                DesignTimeMode = designTime,
                IsIndentingWithTabs = isIndentingWithTabs,
                TabSize = tabSize,
            };
        }

        private static Span GenerateSpan(string text, SpanKind spanKind, int spanIndex, string spanText)
        {
            Span[] spans = GenerateSpans(text, spanKind, spanIndex, spanText);

            return spans[spanIndex];
        }

        private static Span[] GenerateSpans(string text, SpanKind spanKind, int spanIndex, string spanText)
        {
            Assert.True(spanIndex > 0);

            RazorParser parser = new RazorParser(new CSharpCodeParser(), new HtmlMarkupParser());

            Span[] spans;

            using (var reader = new StringReader(text))
            {
                ParserResults results = parser.Parse(reader);
                spans = results.Document.Flatten().ToArray();
            }

            Assert.True(spans.Length > spanIndex);
            Assert.Equal(spanKind, spans[spanIndex].Kind);
            Assert.Equal(spanText, spans[spanIndex].Content);

            return spans;
        }
    }
}
