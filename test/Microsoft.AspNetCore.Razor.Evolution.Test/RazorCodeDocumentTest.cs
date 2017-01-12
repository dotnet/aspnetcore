// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorCodeDocumentTest
    {
        [Fact]
        public void Create()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create();

            // Act
            var code = RazorCodeDocument.Create(source);

            // Assert
            Assert.Same(source, code.Source);
            Assert.NotNull(code.Items);
        }

        [Fact]
        public void Create_WithImportsAndIncludes()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create();

            var imports = new RazorSourceDocument[]
            {
                TestRazorSourceDocument.Create(),
            };

            var includes = new RazorSourceDocument[]
            {
                TestRazorSourceDocument.Create(),
            };

            // Act
            var code = RazorCodeDocument.Create(source, imports, includes);

            // Assert
            Assert.Same(source, code.Source);
            Assert.NotNull(code.Items);

            Assert.NotSame(imports, code.Imports);
            Assert.Collection(imports, d => Assert.Same(imports[0], d));

            Assert.NotSame(includes, code.Includes);
            Assert.Collection(includes, d => Assert.Same(includes[0], d));
        }

        [Fact]
        public void Create_WithImportsAndIncludes_AllsNull()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create();

            // Act
            var code = RazorCodeDocument.Create(source, imports: null, includes: null);

            // Assert
            Assert.Same(source, code.Source);
            Assert.NotNull(code.Items);
            Assert.Empty(code.Imports);
            Assert.Empty(code.Includes);
        }
    }
}
