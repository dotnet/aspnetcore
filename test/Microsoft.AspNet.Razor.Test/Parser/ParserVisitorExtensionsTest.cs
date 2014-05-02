// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.TestCommon;
using Moq;

namespace Microsoft.AspNet.Razor.Test.Parser
{
    public class ParserVisitorExtensionsTest
    {
        [Fact]
        public void VisitThrowsOnNullVisitor()
        {
            ParserVisitor target = null;
            ParserResults results = new ParserResults(new BlockBuilder() { Type = BlockType.Comment }.Build(), new List<RazorError>());

            Assert.ThrowsArgumentNull(() => target.Visit(results), "self");
        }

        [Fact]
        public void VisitThrowsOnNullResults()
        {
            ParserVisitor target = new Mock<ParserVisitor>().Object;
            Assert.ThrowsArgumentNull(() => target.Visit(null), "result");
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
