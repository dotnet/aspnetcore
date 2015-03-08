// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using System;
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
        public void VisitThrowsOnNullVisitor()
        {
            ParserVisitor target = null;
            var errorSink = new ParserErrorSink();
            var results = new ParserResults(new BlockBuilder() { Type = BlockType.Comment }.Build(),
                                            Enumerable.Empty<TagHelperDescriptor>(),
                                            errorSink);

            Assert.Throws<ArgumentNullException>("self", () => target.Visit(results));
        }

        [Fact]
        public void VisitThrowsOnNullResults()
        {
            var target = new Mock<ParserVisitor>().Object;
            Assert.Throws<ArgumentNullException>("result", () => target.Visit(null));
        }

        [Fact]
        public void VisitSendsDocumentToVisitor()
        {
            // Arrange
            Mock<ParserVisitor> targetMock = new Mock<ParserVisitor>();
            var root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            var errorSink = new ParserErrorSink();
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
            var errorSink = new ParserErrorSink();
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
            var errorSink = new ParserErrorSink();
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
