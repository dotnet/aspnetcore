// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Parser
{
    public class ParserVisitorExtensionsTest
    {
        [Fact]
        public void VisitSendsDocumentToVisitor()
        {
            // Arrange
            var targetMock = new Mock<ParserVisitor>();
            var root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            var errorSink = new ErrorSink();
            var results = new ParserResults(root,
                                            Enumerable.Empty<TagHelperDescriptor>(),
                                            errorSink);

            // Act
            targetMock.Object.Visit(results);

            // Assert
            targetMock.Verify(v => v.VisitBlock(root));
        }

        [Fact]
        public void VisitSendsErrorsToVisitor()
        {
            // Arrange
            var targetMock = new Mock<ParserVisitor>();
            var root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            var errorSink = new ErrorSink();
            var errors = new List<RazorError>
            {
                new RazorError("Foo", new SourceLocation(1, 0, 1), length: 3),
                new RazorError("Bar", new SourceLocation(2, 0, 2), length: 3),
            };
            foreach (var error in errors)
            {
                errorSink.OnError(error);
            }
            var results = new ParserResults(root, Enumerable.Empty<TagHelperDescriptor>(), errorSink);

            // Act
            targetMock.Object.Visit(results);

            // Assert
            targetMock.Verify(v => v.VisitError(errors[0]));
            targetMock.Verify(v => v.VisitError(errors[1]));
        }

        [Fact]
        public void VisitCallsOnCompleteWhenAllNodesHaveBeenVisited()
        {
            // Arrange
            var targetMock = new Mock<ParserVisitor>();
            var root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            var errorSink = new ErrorSink();
            errorSink.OnError(new RazorError("Foo", new SourceLocation(1, 0, 1), length: 3));
            errorSink.OnError(new RazorError("Bar", new SourceLocation(2, 0, 2), length: 3));
            var results = new ParserResults(root, Enumerable.Empty<TagHelperDescriptor>(), errorSink);

            // Act
            targetMock.Object.Visit(results);

            // Assert
            targetMock.Verify(v => v.OnComplete());
        }
    }
}
