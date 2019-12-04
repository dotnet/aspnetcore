// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test.Legacy
{
    public class CodeBlockEditHandlerTest
    {
        [Fact]
        public void IsAcceptableReplacement_AcceptableReplacement_ReturnsTrue()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "Hello {world}.");
            var change = new SourceChange(new SourceSpan(0, 5), "H3ll0");

            // Act
            var result = CodeBlockEditHandler.IsAcceptableReplacement(span, change);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAcceptableReplacement_ChangeModifiesInvalidContent_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "Hello {world}.");
            var change = new SourceChange(new SourceSpan(6, 1), "!");

            // Act
            var result = CodeBlockEditHandler.IsAcceptableReplacement(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableReplacement_ChangeContainsInvalidContent_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "Hello {world}.");
            var change = new SourceChange(new SourceSpan(0, 0), "{");

            // Act
            var result = CodeBlockEditHandler.IsAcceptableReplacement(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableReplacement_NotReplace_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "Hello {world}.");
            var change = new SourceChange(new SourceSpan(0, 5), string.Empty);

            // Act
            var result = CodeBlockEditHandler.IsAcceptableReplacement(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableDeletion_ValidChange_ReturnsTrue()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "Hello {world}.");
            var change = new SourceChange(new SourceSpan(0, 5), string.Empty);

            // Act
            var result = CodeBlockEditHandler.IsAcceptableDeletion(span, change);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAcceptableDeletion_InvalidChange_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "Hello {world}.");
            var change = new SourceChange(new SourceSpan(5, 3), string.Empty);

            // Act
            var result = CodeBlockEditHandler.IsAcceptableDeletion(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableDeletion_NotDelete_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "world");
            var change = new SourceChange(new SourceSpan(0, 0), "hello");

            // Act
            var result = CodeBlockEditHandler.IsAcceptableDeletion(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ModifiesInvalidContent_ValidContent_ReturnsFalse()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "Hello {world}.");
            var change = new SourceChange(new SourceSpan(0, 5), string.Empty);

            // Act
            var result = CodeBlockEditHandler.ModifiesInvalidContent(span, change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ModifiesInvalidContent_InvalidContent_ReturnsTrue()
        {
            // Arrange
            var span = GetSpan(SourceLocation.Zero, "Hello {world}.");
            var change = new SourceChange(new SourceSpan(5, 7), string.Empty);

            // Act
            var result = CodeBlockEditHandler.ModifiesInvalidContent(span, change);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAcceptableInsertion_ValidChange_ReturnsTrue()
        {
            // Arrange
            var change = new SourceChange(new SourceSpan(0, 0), "hello");

            // Act
            var result = CodeBlockEditHandler.IsAcceptableInsertion(change);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAcceptableInsertion_InvalidChange_ReturnsFalse()
        {
            // Arrange
            var change = new SourceChange(new SourceSpan(0, 0), "{");

            // Act
            var result = CodeBlockEditHandler.IsAcceptableInsertion(change);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAcceptableInsertion_NotInsert_ReturnsFalse()
        {
            // Arrange
            var change = new SourceChange(new SourceSpan(0, 2), string.Empty);

            // Act
            var result = CodeBlockEditHandler.IsAcceptableInsertion(change);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("{")]
        [InlineData("}")]
        [InlineData("if (true) { }")]
        public void ContainsInvalidContent_InvalidContent_ReturnsTrue(string content)
        {
            // Arrange
            var change = new SourceChange(new SourceSpan(0, 0), content);

            // Act
            var result = CodeBlockEditHandler.ContainsInvalidContent(change);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("var x = true;")]
        [InlineData("if (true) Console.WriteLine('!')")]
        public void ContainsInvalidContent_ValidContent_ReturnsFalse(string content)
        {
            // Arrange
            var change = new SourceChange(new SourceSpan(0, 0), content);

            // Act
            var result = CodeBlockEditHandler.ContainsInvalidContent(change);

            // Assert
            Assert.False(result);
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
    }
}
