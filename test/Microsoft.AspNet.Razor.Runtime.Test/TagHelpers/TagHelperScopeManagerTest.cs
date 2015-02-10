// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.Test.TagHelpers
{
    public class TagHelperScopeManagerTest
    {
        private static readonly Action DefaultStartWritingScope = () => { };
        private static readonly Func<TextWriter> DefaultEndWritingScope = () => new StringWriter();
        private static readonly Func<Task> DefaultExecuteChildContentAsync =
            async () => await Task.FromResult(result: true);

        [Fact]
        public void Begin_CreatesContextWithAppropriateTagName()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                "p",
                selfClosing: false,
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Assert
            Assert.Equal("p", executionContext.TagName);
        }

        [Fact]
        public void Begin_CanNest()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                "p",
                selfClosing: false,
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            executionContext = scopeManager.Begin(
               "div",
               selfClosing: false,
               uniqueId: string.Empty,
               executeChildContentAsync: DefaultExecuteChildContentAsync,
               startWritingScope: DefaultStartWritingScope,
               endWritingScope: DefaultEndWritingScope);

            // Assert
            Assert.Equal("div", executionContext.TagName);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        public void Begin_SetExecutionContextSelfClosing(bool selfClosing)
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                "p",
                selfClosing: selfClosing,
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            // Assert
            Assert.Equal(selfClosing, executionContext.SelfClosing);
        }

    [Fact]
        public void End_ReturnsParentExecutionContext()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                "p",
                selfClosing: false,
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            executionContext = scopeManager.Begin(
               "div",
               selfClosing: false,
               uniqueId: string.Empty,
               executeChildContentAsync: DefaultExecuteChildContentAsync,
               startWritingScope: DefaultStartWritingScope,
               endWritingScope: DefaultEndWritingScope);

            executionContext = scopeManager.End();

            // Assert
            Assert.Equal("p", executionContext.TagName);
        }

        [Fact]
        public void End_ReturnsNullIfNoNestedContext()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();

            // Act
            var executionContext = scopeManager.Begin(
                "p",
                selfClosing: false,
                uniqueId: string.Empty,
                executeChildContentAsync: DefaultExecuteChildContentAsync,
                startWritingScope: DefaultStartWritingScope,
                endWritingScope: DefaultEndWritingScope);

            executionContext = scopeManager.Begin(
               "div",
               selfClosing: false,
               uniqueId: string.Empty,
               executeChildContentAsync: DefaultExecuteChildContentAsync,
               startWritingScope: DefaultStartWritingScope,
               endWritingScope: DefaultEndWritingScope);

            executionContext = scopeManager.End();
            executionContext = scopeManager.End();

            // Assert
            Assert.Null(executionContext);
        }

        [Fact]
        public void End_ThrowsIfNoScope()
        {
            // Arrange
            var scopeManager = new TagHelperScopeManager();
            var expectedError = string.Format(
                "Must call '{2}.{1}' before calling '{2}.{0}'.",
                nameof(TagHelperScopeManager.End),
                nameof(TagHelperScopeManager.Begin),
                nameof(TagHelperScopeManager));

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                scopeManager.End();
            });

            Assert.Equal(expectedError, ex.Message);
        }
    }
}