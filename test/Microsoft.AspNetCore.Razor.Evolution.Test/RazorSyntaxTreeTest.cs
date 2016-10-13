// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Test
{
    public class RazorSyntaxTreeTest
    {
        [Fact]
        public void Parse_CanParseEmptyDocument()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create(string.Empty);

            // Act
            var syntaxTree = RazorSyntaxTree.Parse(source);

            // Assert
            Assert.NotNull(syntaxTree);
            Assert.Empty(syntaxTree.Diagnostics);
        }
    }
}
