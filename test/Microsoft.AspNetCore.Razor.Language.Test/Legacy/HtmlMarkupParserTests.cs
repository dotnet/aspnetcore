using System.Collections.Generic;
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
            HtmlSymbol convertedSymbol = (HtmlSymbol)symbol;

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsDashSymbol(convertedSymbol));
        }

        [Fact]
        public void IsDashSymbol_ReturnsTrueForADashSymbol()
        {
            // Arrange
            HtmlSymbol dashSymbol = new HtmlSymbol("-", HtmlSymbolType.Text);

            // Act & Assert
            Assert.True(HtmlMarkupParser.IsDashSymbol(dashSymbol));
        }

        [Fact]
        public void AcceptAllButLastDoubleHypens_ReturnsTheOnlyDoubleHyphenSymbol()
        {
            // Arrange
            TestHtmlMarkupParser sut = CreateTestParserForContent("-->");

            // Act
            HtmlSymbol symbol = sut.AcceptAllButLastDoubleHypens();

            // Assert
            Assert.Equal(doubleHyphenSymbol, symbol);
            Assert.True(sut.At(HtmlSymbolType.CloseAngle));
            Assert.Equal(doubleHyphenSymbol, sut.PreviousSymbol);
        }

        [Fact]
        public void AcceptAllButLastDoubleHypens_ReturnsTheDoubleHyphenSymbolAfterAcceptingTheDash()
        {
            // Arrange
            TestHtmlMarkupParser sut = CreateTestParserForContent("--->");

            // Act
            HtmlSymbol symbol = sut.AcceptAllButLastDoubleHypens();

            // Assert
            Assert.Equal(doubleHyphenSymbol, symbol);
            Assert.True(sut.At(HtmlSymbolType.CloseAngle));
            Assert.True(HtmlMarkupParser.IsDashSymbol(sut.PreviousSymbol));
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsTrueForEmptyCommentTag()
        {
            // Arrange
            TestHtmlMarkupParser sut = CreateTestParserForContent("---->");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsTrueForValidCommentTag()
        {
            // Arrange
            TestHtmlMarkupParser sut = CreateTestParserForContent("-- Some comment content in here -->");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsTrueForValidCommentTagWithExtraDashesAtClosingTag()
        {
            // Arrange
            TestHtmlMarkupParser sut = CreateTestParserForContent("-- Some comment content in here ----->");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsTrueForValidCommentTagWithExtraInfoAfter()
        {
            // Arrange
            TestHtmlMarkupParser sut = CreateTestParserForContent("-- comment --> the first part is a valid comment without the Open angle and bang symbols");

            // Act & Assert
            Assert.True(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsFalseForNotClosedComment()
        {
            // Arrange
            TestHtmlMarkupParser sut = CreateTestParserForContent("-- not closed comment");

            // Act & Assert
            Assert.False(sut.IsHtmlCommentAhead());
        }

        [Fact]
        public void IsHtmlCommentAhead_ReturnsFalseForCommentWithoutLastClosingAngle()
        {
            // Arrange
            TestHtmlMarkupParser sut = CreateTestParserForContent("-- not closed comment--");

            // Act & Assert
            Assert.False(sut.IsHtmlCommentAhead());
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
