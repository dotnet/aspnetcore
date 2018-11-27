// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class SourceChangeTest
    {
        [Fact]
        public void SourceChange_ConstructorSetsDefaults_WhenNotProvided()
        {
            // Arrange & Act
            var change = new SourceChange(15, 7, "Hello");

            // Assert
            Assert.Equal(15, change.Span.AbsoluteIndex);
            Assert.Equal(-1, change.Span.CharacterIndex);
            Assert.Null(change.Span.FilePath);
            Assert.Equal(7, change.Span.Length);
            Assert.Equal(-1, change.Span.LineIndex);
            Assert.Equal("Hello", change.NewText);
        }

        [Fact]
        public void IsDelete_IsTrue_WhenOldLengthIsPositive_AndNewLengthIsZero()
        {
            // Arrange & Act
            var change = new SourceChange(3, 5, string.Empty);

            // Assert
            Assert.True(change.IsDelete);
        }

        [Fact]
        public void IsInsert_IsTrue_WhenOldLengthIsZero_AndNewLengthIsPositive()
        {
            // Arrange & Act
            var change = new SourceChange(3, 0, "Hello");

            // Assert
            Assert.True(change.IsInsert);
        }

        [Fact]
        public void IsReplace_IsTrue_WhenOldLengthIsPositive_AndNewLengthIsPositive()
        {
            // Arrange & Act
            var change = new SourceChange(3, 5, "Hello");

            // Assert
            Assert.True(change.IsReplace);
        }

        [Fact]
        public void GetEditedContent_ForDelete_ReturnsNewContent()
        {
            // Arrange
            var text = "Hello, World";

            var change = new SourceChange(2, 2, string.Empty);

            // Act
            var result = change.GetEditedContent(text, 1);

            // Act
            Assert.Equal("Hlo, World", result);
        }

        [Fact]
        public void GetEditedContent_ForInsert_ReturnsNewContent()
        {
            // Arrange
            var text = "Hello, World";

            var change = new SourceChange(2, 0, "heyo");

            // Act
            var result = change.GetEditedContent(text, 1);

            // Act
            Assert.Equal("Hheyoello, World", result);
        }

        [Fact]
        public void GetEditedContent_ForReplace_ReturnsNewContent()
        {
            // Arrange
            var text = "Hello, World";

            var change = new SourceChange(2, 2, "heyo");

            // Act
            var result = change.GetEditedContent(text, 1);

            // Act
            Assert.Equal("Hheyolo, World", result);
        }

        [Fact]
        public void GetEditedContent_SyntaxNode_ReturnsNewContent()
        {
            // Arrange
            var builder = SyntaxListBuilder<SyntaxToken>.Create();
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "Hello, "));
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "World"));

            var node = SyntaxFactory.MarkupTextLiteral(builder.ToList()).CreateRed();

            var change = new SourceChange(2, 2, "heyo");

            // Act
            var result = change.GetEditedContent(node);

            // Act
            Assert.Equal("Heheyoo, World", result);
        }

        [Fact]
        public void GetOffSet_SpanIsOwner_ReturnsOffset()
        {
            // Arrange
            var builder = SyntaxListBuilder<SyntaxToken>.Create();
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "Hello, "));
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "World"));

            var node = SyntaxFactory.MarkupTextLiteral(builder.ToList()).CreateRed(null, 13);

            var change = new SourceChange(15, 2, "heyo");

            // Act
            var result = change.GetOffset(node);

            // Act
            Assert.Equal(2, result);
        }

        [Fact]
        public void GetOffSet_SpanIsNotOwnerOfChange_ThrowsException()
        {
            // Arrange
            var builder = SyntaxListBuilder<SyntaxToken>.Create();
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "Hello, "));
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "World"));

            var node = SyntaxFactory.MarkupTextLiteral(builder.ToList()).CreateRed(null, 13);

            var change = new SourceChange(12, 2, "heyo");

            var expected = $"The node '{node}' is not the owner of change '{change}'.";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => { change.GetOffset(node); });
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public void GetOrigninalText_SpanIsOwner_ReturnsContent()
        {
            // Arrange
            var builder = SyntaxListBuilder<SyntaxToken>.Create();
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "Hello, "));
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "World"));

            var node = SyntaxFactory.MarkupTextLiteral(builder.ToList()).CreateRed(null, 13);

            var change = new SourceChange(15, 2, "heyo");

            // Act
            var result = change.GetOriginalText(node);

            // Act
            Assert.Equal("ll", result);
        }

        [Fact]
        public void GetOrigninalText_SpanIsOwner_ReturnsContent_ZeroLengthSpan()
        {
            // Arrange
            var builder = SyntaxListBuilder<SyntaxToken>.Create();
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "Hello, "));
            builder.Add(SyntaxFactory.Token(SyntaxKind.Marker, "World"));

            var node = SyntaxFactory.MarkupTextLiteral(builder.ToList()).CreateRed(null, 13);

            var change = new SourceChange(15, 0, "heyo");

            // Act
            var result = change.GetOriginalText(node);

            // Act
            Assert.Equal(string.Empty, result);
        }
    }
}
