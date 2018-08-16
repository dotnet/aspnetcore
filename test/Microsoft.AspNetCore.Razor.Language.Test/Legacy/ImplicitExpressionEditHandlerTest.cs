// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class ImplicitExpressionEditHandlerTest
    {
        [Fact]
        public void IsAcceptableDeletionInBalancedParenthesis_DeletionStartNotInBalancedParenthesis_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "(Hell)(o)");
            var change = new SourceChange(new SourceSpan(6, 1), string.Empty);

            // Act
            var result = ImplicitExpressionEditHandler.IsAcceptableDeletionInBalancedParenthesis(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableDeletionInBalancedParenthesis_DeletionEndNotInBalancedParenthesis_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "(Hell)(o)");
            var change = new SourceChange(new SourceSpan(5, 1), string.Empty);

            // Act
            var result = ImplicitExpressionEditHandler.IsAcceptableDeletionInBalancedParenthesis(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableDeletionInBalancedParenthesis_DeletionOverlapsBalancedParenthesis_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "(Hell)(o)");
            var change = new SourceChange(new SourceSpan(5, 2), string.Empty);

            // Act
            var result = ImplicitExpressionEditHandler.IsAcceptableDeletionInBalancedParenthesis(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableDeletionInBalancedParenthesis_DeletionDoesNotImpactBalancedParenthesis_ReturnsTrue()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "(H(ell)o)");
            var change = new SourceChange(new SourceSpan(3, 3), string.Empty);

            // Act
            var result = ImplicitExpressionEditHandler.IsAcceptableDeletionInBalancedParenthesis(span, change);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("(")]
        [InlineData(")")]
        public void IsAcceptableInsertionInBalancedParenthesis_ReturnsFalseIfChangeIsParenthesis(string changeText)
        {
            // Arrange
            var change = new SourceChange(0, 1, changeText);

            // Act
            var result = ImplicitExpressionEditHandler.IsAcceptableInsertionInBalancedParenthesis(null, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryUpdateCountFromContent_SingleLeftParenthesis_CountsCorrectly()
        {
            // Arrange
            var content = "(";
            var count = 0;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateCountFromContent(content, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(1, count);
        }

        [Fact]
        public void TryUpdateCountFromContent_SingleRightParenthesis_CountsCorrectly()
        {
            // Arrange
            var content = ")";
            var count = 2;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateCountFromContent(content, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(1, count);
        }

        [Fact]
        public void TryUpdateCountFromContent_CorrectCount_ReturnsTrue()
        {
            // Arrange
            var content = "\"(()(";
            var count = 0;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateCountFromContent(content, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(2, count);
        }

        [Fact]
        public void TryUpdateCountFromContent_ExistingCountAndNonParenthesisContent_ReturnsTrue()
        {
            // Arrange
            var content = "'(abc)de)fg";
            var count = 1;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateCountFromContent(content, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(0, count);
        }

        [Fact]
        public void TryUpdateCountFromContent_InvalidParenthesis_ReturnsFalse()
        {
            // Arrange
            var content = "'())))))";
            var count = 4;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateCountFromContent(content, ref count);

            // Assert
            Assert.False(result);
            Assert.Equal(4, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_SingleLeftParenthesis_CountsCorrectly()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.LeftParenthesis, "(");
            var count = 0;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(1, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_SingleRightParenthesis_CountsCorrectly()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.RightParenthesis, ")");
            var count = 2;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(1, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_IncompleteStringLiteral_CountsCorrectly()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"((");
            var count = 2;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(4, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_IncompleteCharacterLiteral_CountsCorrectly()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'((");
            var count = 2;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(4, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_CompleteStringLiteral_CountsCorrectly()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"((\"");
            var count = 2;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(2, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_CompleteCharacterLiteral_CountsCorrectly()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'('");
            var count = 2;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.True(result);
            Assert.Equal(2, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_InvalidParenthesis_ReturnsFalse()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.RightParenthesis, ")");
            var count = 0;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.False(result);
            Assert.Equal(0, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_InvalidParenthesisStringLiteral_ReturnsFalse()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.StringLiteral, "\")");
            var count = 0;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.False(result);
            Assert.Equal(0, count);
        }

        [Fact]
        public void TryUpdateBalanceCount_InvalidParenthesisCharacterLiteral_ReturnsFalse()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "')");
            var count = 0;

            // Act
            var result = ImplicitExpressionEditHandler.TryUpdateBalanceCount(token, ref count);

            // Assert
            Assert.False(result);
            Assert.Equal(0, count);
        }

        [Fact]
        public void ContainsPosition_AtStartOfToken_ReturnsTrue()
        {
            // Arrange
            var token = GetTokens(new SourceLocation(4, 1, 2), "hello").Single();

            // Act
            var result = ImplicitExpressionEditHandler.ContainsPosition(4, token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ContainsPosition_InsideOfToken_ReturnsTrue()
        {
            // Arrange
            var token = GetTokens(new SourceLocation(4, 1, 2), "hello").Single();

            // Act
            var result = ImplicitExpressionEditHandler.ContainsPosition(6, token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ContainsPosition_AtEndOfToken_ReturnsFalse()
        {
            // Arrange
            var token = GetTokens(new SourceLocation(4, 1, 2), "hello").Single();

            // Act
            var result = ImplicitExpressionEditHandler.ContainsPosition(9, token);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(2)]
        public void ContainsPosition_OutsideOfToken_ReturnsFalse(int position)
        {
            // Arrange
            var token = GetTokens(new SourceLocation(4, 1, 2), "hello").Single();

            // Act
            var result = ImplicitExpressionEditHandler.ContainsPosition(position, token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsInsideParenthesis_SurroundedByCompleteParenthesis_ReturnsFalse()
        {
            // Arrange
            var tokens = GetTokens(SourceLocation.Zero, "(hello)point(world)");

            // Act
            var result = ImplicitExpressionEditHandler.IsInsideParenthesis(9, tokens);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsInsideParenthesis_InvalidParenthesis_ReturnsFalse()
        {
            // Arrange
            var tokens = GetTokens(SourceLocation.Zero, "(hello))point)");

            // Act
            var result = ImplicitExpressionEditHandler.IsInsideParenthesis(10, tokens);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsInsideParenthesis_NoParenthesis_ReturnsFalse()
        {
            // Arrange
            var tokens = GetTokens(SourceLocation.Zero, "Hello World");

            // Act
            var result = ImplicitExpressionEditHandler.IsInsideParenthesis(3, tokens);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsInsideParenthesis_InBalancedParenthesis_ReturnsTrue()
        {
            // Arrange
            var tokens = GetTokens(SourceLocation.Zero, "Foo(GetValue(), DoSomething(point))");

            // Act
            var result = ImplicitExpressionEditHandler.IsInsideParenthesis(30, tokens);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("(")]
        [InlineData(")")]
        public void IsAcceptableInsertionInBalancedParenthesis_InsertingParenthesis_ReturnsFalse(string text)
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "(Hello World)");
            var change = new SourceChange(new SourceSpan(3, 0), text);

            // Act
            var result = ImplicitExpressionEditHandler.IsAcceptableInsertionInBalancedParenthesis(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableInsertionInBalancedParenthesis_UnbalancedParenthesis_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "(Hello");
            var change = new SourceChange(new SourceSpan(6, 0), " World");

            // Act
            var result = ImplicitExpressionEditHandler.IsAcceptableInsertionInBalancedParenthesis(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableInsertionInBalancedParenthesis_BalancedParenthesis_ReturnsTrue()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "(Hello)");
            var change = new SourceChange(new SourceSpan(6, 0), " World");

            // Act
            var result = ImplicitExpressionEditHandler.IsAcceptableInsertionInBalancedParenthesis(span, change);

            // Assert
            Assert.True(result);
        }

        private static Span GetSpan(SourceLocation start, string content)
        {
            var spanBuilder = new SpanBuilder(start);
            var tokens = CSharpLanguageCharacteristics.Instance.TokenizeString(content).ToArray();
            foreach (var token in tokens)
            {
                spanBuilder.Accept(token);
            }
            var span = spanBuilder.Build();

            return span;
        }

        private static IReadOnlyList<SyntaxToken> GetTokens(SourceLocation start, string content)
        {
            var parent = GetSpan(start, content);
            return parent.Tokens;
        }
    }
}
