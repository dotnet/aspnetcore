using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test.Legacy
{
    public class HtmlMarkupParserTests
    {
        private static readonly SyntaxToken doubleHyphenToken = SyntaxFactory.Token(SyntaxKind.DoubleHyphen, "--");

        public static IEnumerable<object[]> NonDashTokens
        {
            get
            {
                yield return new[] { SyntaxFactory.Token(SyntaxKind.DoubleHyphen, "--") };
                yield return new[] { SyntaxFactory.Token(SyntaxKind.Text, "asdf") };
                yield return new[] { SyntaxFactory.Token(SyntaxKind.CloseAngle, ">") };
                yield return new[] { SyntaxFactory.Token(SyntaxKind.OpenAngle, "<") };
                yield return new[] { SyntaxFactory.Token(SyntaxKind.Bang, "!") };
            }
        }

        [Theory]
        [MemberData(nameof(NonDashTokens))]
        public void IsHyphen_ReturnsFalseForNonDashToken(object token)
        {
            // Arrange
            var convertedToken = (SyntaxToken)token;

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsHyphen(convertedToken));
        }

        [Fact]
        public void IsHyphen_ReturnsTrueForADashToken()
        {
            // Arrange
            var dashToken = SyntaxFactory.Token(SyntaxKind.Text, "-");

            // Act & Assert
            Assert.True(HtmlMarkupParser.IsHyphen(dashToken));
        }

        [Fact]
        public void AcceptAllButLastDoubleHypens_ReturnsTheOnlyDoubleHyphenToken()
        {
            // Arrange
            var sut = CreateTestParserForContent("-->");

            // Act
            var token = sut.AcceptAllButLastDoubleHyphens();

            // Assert
            Assert.True(doubleHyphenToken.IsEquivalentTo(token));
            Assert.True(sut.At(SyntaxKind.CloseAngle));
            Assert.True(doubleHyphenToken.IsEquivalentTo(sut.PreviousToken));
        }

        [Fact]
        public void AcceptAllButLastDoubleHypens_ReturnsTheDoubleHyphenTokenAfterAcceptingTheDash()
        {
            // Arrange
            var sut = CreateTestParserForContent("--->");

            // Act
            var token = sut.AcceptAllButLastDoubleHyphens();

            // Assert
            Assert.True(doubleHyphenToken.IsEquivalentTo(token));
            Assert.True(sut.At(SyntaxKind.CloseAngle));
            Assert.True(HtmlMarkupParser.IsHyphen(sut.PreviousToken));
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
            var sut = CreateTestParserForContent("-- comment --> the first part is a valid comment without the Open angle and bang tokens");

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
            var expectedToken1 = SyntaxFactory.Token(SyntaxKind.Text, "a");
            var sequence = Enumerable.Range((int)'a', 26).Select(item => SyntaxFactory.Token(SyntaxKind.Text, ((char)item).ToString()));

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsCommentContentEndingInvalid(sequence));
        }

        [Fact]
        public void IsCommentContentEndingInvalid_ReturnsTrueForDisallowedContent()
        {
            // Arrange
            var expectedToken1 = SyntaxFactory.Token(SyntaxKind.Text, "a");
            var sequence = new[]
            {
                SyntaxFactory.Token(SyntaxKind.OpenAngle, "<"),
                SyntaxFactory.Token(SyntaxKind.Bang, "!"),
                SyntaxFactory.Token(SyntaxKind.Text, "-")
            };

            // Act & Assert
            Assert.True(HtmlMarkupParser.IsCommentContentEndingInvalid(sequence));
        }

        [Fact]
        public void IsCommentContentEndingInvalid_ReturnsFalseForEmptyContent()
        {
            // Arrange
            var expectedToken1 = SyntaxFactory.Token(SyntaxKind.Text, "a");
            var sequence = Array.Empty<SyntaxToken>();

            // Act & Assert
            Assert.False(HtmlMarkupParser.IsCommentContentEndingInvalid(sequence));
        }

        private class TestHtmlMarkupParser : HtmlMarkupParser
        {
            public new SyntaxToken PreviousToken
            {
                get => base.PreviousToken;
            }

            public new bool IsHtmlCommentAhead()
            {
                return base.IsHtmlCommentAhead();
            }

            public TestHtmlMarkupParser(ParserContext context) : base(context)
            {
                this.EnsureCurrent();
            }

            public new SyntaxToken AcceptAllButLastDoubleHyphens()
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
