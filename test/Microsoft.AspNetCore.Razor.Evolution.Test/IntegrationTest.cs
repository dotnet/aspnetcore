// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class IntegrationTest
    {
        [Fact]
        public void BuildEngine_CallProcess()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = RazorCodeDocument.Create(TestRazorSourceDocument.Create());

            // Act
            engine.Process(document);

            // Assert
            Assert.NotNull(document.GetSyntaxTree());
        }
    }
}
