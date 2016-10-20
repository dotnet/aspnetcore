// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultRazorCodeDocumentTest
    {
        [Fact]
        public void Ctor()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create();

            // Act
            var code = new DefaultRazorCodeDocument(source);

            // Assert
            Assert.Same(source, code.Source);
            Assert.NotNull(code.Items);
        }
    }
}
