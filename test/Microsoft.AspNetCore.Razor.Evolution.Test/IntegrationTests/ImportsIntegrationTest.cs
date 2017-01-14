// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    public class ImportsIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void BasicImports()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }
    }
}
