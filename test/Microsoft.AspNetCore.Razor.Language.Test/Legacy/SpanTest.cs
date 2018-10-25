// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class SpanTest
    {
        [Fact]
        public void ReplaceWith_ResetsLength()
        {
            // Arrange
            var builder = new SpanBuilder(SourceLocation.Zero);
            builder.Accept(SyntaxFactory.Token(SyntaxKind.Text, "hello"));
            var span = builder.Build();
            var newBuilder = new SpanBuilder(SourceLocation.Zero);
            newBuilder.Accept(SyntaxFactory.Token(SyntaxKind.Text, "hi"));
            var originalLength = span.Length;

            // Act
            span.ReplaceWith(newBuilder);

            // Assert
            Assert.Equal(5, originalLength);
            Assert.Equal(2, span.Length);
        }

        // Note: This is more of an integration-like test. However, it's valuable to determine
        // that the Span's ReplaceWith code is properly propogating change notifications to parents.
        [Fact]
        public void ReplaceWith_NotifiesParentChildHasChanged()
        {
            // Arrange
            var spanBuilder = new SpanBuilder(SourceLocation.Zero);
            spanBuilder.Accept(SyntaxFactory.Token(SyntaxKind.Text, "hello"));
            var span = spanBuilder.Build();
            var blockBuilder = new BlockBuilder()
            {
                Type = BlockKindInternal.Markup,
            };
            blockBuilder.Children.Add(span);
            var block = blockBuilder.Build();
            span.Parent = block;
            var originalBlockLength = block.Length;
            var newSpanBuilder = new SpanBuilder(SourceLocation.Zero);
            newSpanBuilder.Accept(SyntaxFactory.Token(SyntaxKind.Text, "hi"));

            // Act
            span.ReplaceWith(newSpanBuilder);

            // Assert
            Assert.Equal(5, originalBlockLength);
            Assert.Equal(2, block.Length);
        }

        [Fact]
        public void Clone_ClonesSpan()
        {
            // Arrange
            var spanBuilder = new SpanBuilder(new SourceLocation(1, 2, 3))
            {
                EditHandler = new SpanEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString),
                Kind = SpanKindInternal.Transition,
                ChunkGenerator = new ExpressionChunkGenerator(),
            };
            spanBuilder.Accept(SyntaxFactory.Token(SyntaxKind.Transition, "@"));
            var span = spanBuilder.Build();

            // Act
            var copy = (Span)span.Clone();

            // Assert
            Assert.Equal(span, copy);
            Assert.NotSame(span, copy);
        }
    }
}
