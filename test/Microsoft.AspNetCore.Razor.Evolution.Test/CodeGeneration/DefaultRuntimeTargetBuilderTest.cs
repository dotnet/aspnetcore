// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public class DefaultRuntimeTargetBuilderTest
    {
        [Fact]
        public void Build_CreatesDefaultRuntimeTarget()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorParserOptions.CreateDefaultOptions();

            var builder = new DefaultRuntimeTargetBuilder(codeDocument, options);

            // Act
            var target = builder.Build();

            // Assert
            Assert.IsType<DefaultRuntimeTarget>(target);
        }
    }
}
