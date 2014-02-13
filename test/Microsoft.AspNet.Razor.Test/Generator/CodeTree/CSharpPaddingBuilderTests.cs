using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CSharpPaddingBuilderTests
    {
        [Fact]
        public void CalculatePaddingForEmptySpanReturnsZero()
        {
            // Arrange
            RazorEngineHost host = CreateHost(designTime: true);

            Span span = new Span(new SpanBuilder());

            var paddingBuilder = new CSharpPaddingBuilder(host);

            // Act
            int padding = paddingBuilder.CalculatePadding(span);

            // Assert
            Assert.Equal(0, padding);
        }

        [Theory]
        [PropertyData("SpacePropertyData")]
        public void CalculatePaddingForEmptySpanWith4Spaces(bool designTime, bool isIndentingWithTabs, int tabSize)
        {
            // Arrange
            RazorEngineHost host = CreateHost(designTime, isIndentingWithTabs, tabSize);

            Span span = GenerateSpan(@"    @{", SpanKind.Code, 3, "");

            var paddingBuilder = new CSharpPaddingBuilder(host);

            // Act
            int padding = paddingBuilder.CalculatePadding(span);

            // Assert
            Assert.Equal(6, padding);
        }

        [Theory]
        [PropertyData("SpacePropertyData")]
        public void CalculatePaddingForIfSpanWith5Spaces(bool designTime, bool isIndentingWithTabs, int tabSize)
        {
            // Arrange
            RazorEngineHost host = CreateHost(designTime, isIndentingWithTabs, tabSize);

            Span span = GenerateSpan(@"    @if (true)", SpanKind.Code, 2, "if (true)");

            var paddingBuilder = new CSharpPaddingBuilder(host);

            // Act
            int padding = paddingBuilder.CalculatePadding(span);

            // Assert
            Assert.Equal(5, padding);
        }

        // 4 padding should result in 4 spaces. Where in the previous test (5 spaces) should result in 1 tab.
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
            // Arrange
            RazorEngineHost host = CreateHost(designTime, isIndentingWithTabs, tabSize);

            // no new lines involved
            Span spanFlat = GenerateSpan("    @if (true)", SpanKind.Code, 2, "if (true)");
            Span spanNewlines = GenerateSpan("\t<div>\r\n    @if (true)", SpanKind.Code, 3, "if (true)");

            var paddingBuilder = new CSharpPaddingBuilder(host);

            // Act
            string paddingFlat = paddingBuilder.BuildStatementPadding(spanFlat);


            string paddingNewlines = paddingBuilder.BuildStatementPadding(spanNewlines);

            // Assert
            string code = " if (true)";
            VerifyPadded(numTabs, numSpaces, code, paddingFlat);
            VerifyPadded(numTabs, numSpaces, code, paddingNewlines);
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
            // Arrange
            RazorEngineHost host = CreateHost(designTime, isIndentingWithTabs, tabSize);

            // no new lines involved
            Span spanFlat = GenerateSpan("\t\t@if (true)", SpanKind.Code, 2, "if (true)");
            Span spanNewlines = GenerateSpan("\t<div>\r\n\t\t@if (true)", SpanKind.Code, 3, "if (true)");

            var paddingBuilder = new CSharpPaddingBuilder(host);

            // Act
            string paddingFlat = paddingBuilder.BuildStatementPadding(spanFlat);
            string paddingNewlines = paddingBuilder.BuildStatementPadding(spanNewlines);

            // Assert
            string code = " if (true)";
            VerifyPadded(numTabs, numSpaces, code, paddingFlat);
            VerifyPadded(numTabs, numSpaces, code, paddingNewlines);
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
            // Arrange
            RazorEngineHost host = CreateHost(designTime, isIndentingWithTabs, tabSize);

            string text = "\r\n<html>\r\n<body>\r\n\t\t@if (true) { \r\n</body>\r\n</html>";

            Span span = GenerateSpan(text, SpanKind.Code, 3, "if (true) { \r\n");

            var paddingBuilder = new CSharpPaddingBuilder(host);

            // Act
            string padding = paddingBuilder.BuildStatementPadding(span);

            // Assert
            string code = " if (true) { \r\n";

            VerifyPadded(numTabs, numSpaces, code, padding);
        }

        private static void VerifyPadded(int numTabs, int numSpaces, string code, string padding)
        {
            string padded = padding + code;
            string expectedPadding = new string('\t', numTabs) + new string(' ', numSpaces);

            Assert.Equal(expectedPadding, padding);
            Assert.Equal(numTabs + numSpaces + code.Length, padded.Length);
            Assert.Equal(numSpaces + numTabs, padding.Length);
        }

        public static IEnumerable<object[]> SpacePropertyData
        {
            get
            {
                yield return new object[] { true, false, 4 };
                yield return new object[] { true, false, 2 };
                yield return new object[] { false, true, 4 };
                yield return new object[] { false, true, 2 };
                yield return new object[] { false, false, 4 };
                yield return new object[] { false, false, 2 };
                yield return new object[] { true, true, 4 };
                yield return new object[] { true, true, 2 };
                yield return new object[] { true, true, 1 };
                yield return new object[] { true, true, 0 };
            }
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

            var parser = new RazorParser(new CSharpCodeParser(), new HtmlMarkupParser());

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
