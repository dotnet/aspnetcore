// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
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
            ParserResults results = new ParserResults(new BlockBuilder() { Type = BlockType.Comment }.Build(), new List<RazorError>());

            Assert.Throws<ArgumentNullException>("self", () => target.Visit(results));
        }

        [Fact]
        public void VisitThrowsOnNullResults()
        {
            ParserVisitor target = new Mock<ParserVisitor>().Object;
            Assert.Throws<ArgumentNullException>("result", () => target.Visit(null));
        }

        [Fact]
        public void VisitSendsDocumentToVisitor()
        {
            // Arrange
            Mock<ParserVisitor> targetMock = new Mock<ParserVisitor>();
            Block root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            ParserResults results = new ParserResults(root, new List<RazorError>());

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
            Block root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            List<RazorError> errors = new List<RazorError>() {
                new RazorError("Foo", 1, 0, 1),
                new RazorError("Bar", 2, 0, 2)
            };
            ParserResults results = new ParserResults(root, errors);

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
            Block root = new BlockBuilder() { Type = BlockType.Comment }.Build();
            List<RazorError> errors = new List<RazorError>() {
                new RazorError("Foo", 1, 0, 1),
                new RazorError("Bar", 2, 0, 2)
            };
            ParserResults results = new ParserResults(root, errors);

            // Act
            targetMock.Object.Visit(results);

            // Assert
            targetMock.Verify(v => v.OnComplete());
        }
    }
}
