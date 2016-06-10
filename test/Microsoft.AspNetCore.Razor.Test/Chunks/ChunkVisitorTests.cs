// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Chunks
{
    public class ChunkVisitorTests
    {
        [Fact]
        public void Accept_InvokesAppropriateOverload()
        {
            // Arrange
            var chunks = new Chunk[] { new LiteralChunk(), new StatementChunk() };
            var visitor = CreateVisitor();

            // Act
            visitor.Object.Accept(chunks);

            // Assert
            visitor.Protected().Verify("Visit", Times.AtMostOnce(), chunks[0]);
            visitor.Protected().Verify("Visit", Times.AtMostOnce(), chunks[1]);
        }

        private static Mock<ChunkVisitor<CodeWriter>> CreateVisitor()
        {
            var codeGeneratorContext = new CodeGeneratorContext(
                new RazorEngineHost(new CSharpRazorCodeLanguage()),
                "myclass",
                "myns",
                string.Empty,
                shouldGenerateLinePragmas: false,
                errorSink: new ErrorSink());
            var writer = Mock.Of<CodeWriter>();
            return new Mock<ChunkVisitor<CodeWriter>>(writer, codeGeneratorContext);
        }

        private class MyTestChunk : Chunk
        {
        }
    }
}
