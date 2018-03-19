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
        public void IsHyphen_ReturnsFalseForNonDashSymbol(object symbol)
        {
            // Arrange
            var convertedSymbol = (HtmlSymbol)symbol;

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsHyphen(convertedSymbol));
        }

        [Fact]
        public void IsHyphen_ReturnsTrueForADashSymbol()
        {
            // Arrange
            var dashSymbol = new HtmlSymbol("-", HtmlSymbolType.Text);

            // Act & Assert
            Assert.True(HtmlMarkupParser.IsHyphen(dashSymbol));
        }

        [Fact]
        public void AcceptAllButLastDoubleHypens_ReturnsTheOnlyDoubleHyphenSymbol()
        {
            // Arrange
            var sut = CreateTestParserForContent("-->");

            // Act
            var symbol = sut.AcceptAllButLastDoubleHyphens();

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
            var symbol = sut.AcceptAllButLastDoubleHyphens();

            // Assert
            Assert.Equal(doubleHyphenSymbol, symbol);
            Assert.True(sut.At(HtmlSymbolType.CloseAngle));
            Assert.True(HtmlMarkupParser.IsHyphen(sut.PreviousSymbol));
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
        public void IsHtmlCommentAhead_ReturnsFalseForContentWithBadEndingAndExtraDash()
        {
            // Arrange
            var sut = CreateTestParserForContent("-- Some comment content in here <!--->");

            // Act & Assert
            Assert.False(sut.IsHtmlCommentAhead());
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
        public void IsHtmlCommentAhead_ReturnsTrueForCommentWithCodeInside()
        {
            // Arrange
            var sut = CreateTestParserForContent("-- not closed @DateTime.Now comment-->");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsCommentContentEndingInvalid_ReturnsFalseForAllowedContent()
        {
            // Arrange
            var expectedSymbol1 = new HtmlSymbol("a", HtmlSymbolType.Text);
            var sequence = Enumerable.Range((int)'a', 26).Select(item => new HtmlSymbol(((char)item).ToString(), HtmlSymbolType.Text));

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsCommentContentEndingInvalid(sequence));
        }

        [Fact]
        public void IsCommentContentEndingInvalid_ReturnsTrueForDisallowedContent()
        {
            // Arrange
            var expectedSymbol1 = new HtmlSymbol("a", HtmlSymbolType.Text);
            var sequence = new[] { new HtmlSymbol("<", HtmlSymbolType.OpenAngle), new HtmlSymbol("!", HtmlSymbolType.Bang), new HtmlSymbol("-", HtmlSymbolType.Text) };

            // Act & Assert
            Assert.True(HtmlMarkupParser.IsCommentContentEndingInvalid(sequence));
        }

        [Fact]
        public void IsCommentContentEndingInvalid_ReturnsFalseForEmptyContent()
        {
            // Arrange
            var expectedSymbol1 = new HtmlSymbol("a", HtmlSymbolType.Text);
            var sequence = Array.Empty<HtmlSymbol>();

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsCommentContentEndingInvalid(sequence));
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

            public new HtmlSymbol AcceptAllButLastDoubleHyphens()
            {
                return base.AcceptAllButLastDoubleHyphens();
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