using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test.Legacy
{
    public class HtmlMarkupParserTests
    {
        private static readonly HtmlSymbol doubleHyphenSymbol = new HtmlSymbol("--", HtmlSymbolType.DoubleHyphen);

        public static IEnumerable<object[]> NonDashSymbols
        {
            get
            {
                yield return new[] { new HtmlSymbol("--", HtmlSymbolType.DoubleHyphen) };
                yield return new[] { new HtmlSymbol("asdf", HtmlSymbolType.Text) };
                yield return new[] { new HtmlSymbol(">", HtmlSymbolType.CloseAngle) };
                yield return new[] { new HtmlSymbol("<", HtmlSymbolType.OpenAngle) };
                yield return new[] { new HtmlSymbol("!", HtmlSymbolType.Bang) };
            }
        }

        [Theory]
        [MemberData(nameof(NonDashSymbols))]
        public void IsDashSymbol_ReturnsFalseForNonDashSymbol(object symbol)
        {
            // Arrange
            var convertedSymbol = (HtmlSymbol)symbol;

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsDashSymbol(convertedSymbol));
        }

        [Fact]
        public void IsDashSymbol_ReturnsTrueForADashSymbol()
        {
            // Arrange
            var dashSymbol = new HtmlSymbol("-", HtmlSymbolType.Text);

            // Act & Assert
            Assert.True(HtmlMarkupParser.IsDashSymbol(dashSymbol));
        }

        [Fact]
        public void AcceptAllButLastDoubleHypens_ReturnsTheOnlyDoubleHyphenSymbol()
        {
            // Arrange
            var sut = CreateTestParserForContent("-->");

            // Act
            var symbol = sut.AcceptAllButLastDoubleHypens();

            // Assert
            Assert.Equal(doubleHyphenSymbol, symbol);
            Assert.True(sut.At(HtmlSymbolType.CloseAngle));
            Assert.Equal(doubleHyphenSymbol, sut.PreviousSymbol);
        }

        [Fact]
        public void AcceptAllButLastDoubleHypens_ReturnsTheDoubleHyphenSymbolAfterAcceptingTheDash()
        {
            // Arrange
            var sut = CreateTestParserForContent("--->");

            // Act
            var symbol = sut.AcceptAllButLastDoubleHypens();

            // Assert
            Assert.Equal(doubleHyphenSymbol, symbol);
            Assert.True(sut.At(HtmlSymbolType.CloseAngle));
            Assert.True(HtmlMarkupParser.IsDashSymbol(sut.PreviousSymbol));
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsTrueForEmptyCommentTag()
        {
            // Arrange
            var sut = CreateTestParserForContent("---->");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsTrueForValidCommentTag()
        {
            // Arrange
            var sut = CreateTestParserForContent("-- Some comment content in here -->");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsTrueForValidCommentTagWithExtraDashesAtClosingTag()
        {
            // Arrange
            var sut = CreateTestParserForContent("-- Some comment content in here ----->");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsTrueForValidCommentTagWithExtraInfoAfter()
        {
            // Arrange
            var sut = CreateTestParserForContent("-- comment --> the first part is a valid comment without the Open angle and bang symbols");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsFalseForNotClosedComment()
        {
            // Arrange
            var sut = CreateTestParserForContent("-- not closed comment");

            // Act & Assert
            Assert.False(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsFalseForCommentWithoutLastClosingAngle()
        {
            // Arrange
            var sut = CreateTestParserForContent("-- not closed comment--");

            // Act & Assert
            Assert.False(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsCommentContentDisallowed_ReturnsFalseForAllowedContent()
        {
            // Arrange
            var expectedSymbol1 = new HtmlSymbol("a", HtmlSymbolType.Text);
            var sequence = Enumerable.Range((int)'a', 26).Select(item => new HtmlSymbol(((char)item).ToString(), HtmlSymbolType.Text));

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsCommentContentDisallowed(sequence));
        }

        [Fact]
        public void IsCommentContentDisallowed_ReturnsTrueForDisallowedContent()
        {
            // Arrange
            var expectedSymbol1 = new HtmlSymbol("a", HtmlSymbolType.Text);
            var sequence = new[] { new HtmlSymbol("<", HtmlSymbolType.OpenAngle), new HtmlSymbol("!", HtmlSymbolType.Bang), new HtmlSymbol("-", HtmlSymbolType.Text) };

            // Act & Assert
            Assert.True(HtmlMarkupParser.IsCommentContentDisallowed(sequence));
        }

        [Fact]
        public void IsCommentContentDisallowed_ReturnsFalseForEmptyContent()
        {
            // Arrange
            var expectedSymbol1 = new HtmlSymbol("a", HtmlSymbolType.Text);
            var sequence = Array.Empty<HtmlSymbol>();

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsCommentContentDisallowed(sequence));
        }

        private class TestHtmlMarkupParser : HtmlMarkupParser
        {
            public new HtmlSymbol PreviousSymbol
            {
                get => base.PreviousSymbol;
            }

            public new bool IsHtmlCommentAhead()
            {
                return base.IsHtmlCommentAhead();
            }

            public TestHtmlMarkupParser(ParserContext context) : base(context)
            {
                this.EnsureCurrent();
            }

            public new HtmlSymbol AcceptAllButLastDoubleHypens()
            {
                return base.AcceptAllButLastDoubleHypens();
            }

            public override void BuildSpan(SpanBuilder span, SourceLocation start, string content)
            {
                base.BuildSpan(span, start, content);
            }
        }

        private static TestHtmlMarkupParser CreateTestParserForContent(string content)
        {
            var source = TestRazorSourceDocument.Create(content);
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            return new TestHtmlMarkupParser(context);
        }
    }
}
