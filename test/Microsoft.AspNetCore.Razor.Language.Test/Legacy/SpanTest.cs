// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class SpanTest
    {
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
            spanBuilder.Accept(new CSharpSymbol("@", CSharpSymbolType.Transition));
            var span = spanBuilder.Build();

            // Act
            var copy = (Span)span.Clone();

            // Assert
            Assert.Equal(span, copy);
            Assert.NotSame(span, copy);
        }
    }
}
