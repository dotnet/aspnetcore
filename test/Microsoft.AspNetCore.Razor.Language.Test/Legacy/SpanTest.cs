// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            builder.Accept(new HtmlSymbol("hello", HtmlSymbolType.Text));
            var span = builder.Build();
            var newBuilder = new SpanBuilder(SourceLocation.Zero);
            newBuilder.Accept(new HtmlSymbol("hi", HtmlSymbolType.Text));
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
            spanBuilder.Accept(new HtmlSymbol("hello", HtmlSymbolType.Text));
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
            newSpanBuilder.Accept(new HtmlSymbol("hi", HtmlSymbolType.Text));

            // Act
            span.ReplaceWith(newSpanBuilder);

            // Assert
            Assert.Equal(5, originalBlockLength);
            Assert.Equal(2, block.Length);
        }
    }
}
