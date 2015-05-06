// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser
{
    public class ParserVisitorExtensionsTest
    {
        [Fact]
        public void VisitSendsDocumentToVisitor()
        {
            // Arrange
            Mock<ParserVisitor> targetMock = new Mock<ParserVisitor>();
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
            Mock<ParserVisitor> targetMock = new Mock<ParserVisitor>();
            var root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            var errorSink = new ErrorSink();
            List<RazorError> errors = new List<RazorError>
            {
                new RazorError("Foo", 1, 0, 1),
                new RazorError("Bar", 2, 0, 2),
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
            Mock<ParserVisitor> targetMock = new Mock<ParserVisitor>();
            var root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            var errorSink = new ErrorSink();
            errorSink.OnError(new RazorError("Foo", 1, 0, 1));
            errorSink.OnError(new RazorError("Bar", 2, 0, 2));
            var results = new ParserResults(root, Enumerable.Empty<TagHelperDescriptor>(), errorSink);

            // Act
            targetMock.Object.Visit(results);

            // Assert
            targetMock.Verify(v => v.OnComplete());
        }
    }
}
#endif
