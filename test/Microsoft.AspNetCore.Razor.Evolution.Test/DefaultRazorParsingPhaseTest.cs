// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultRazorParsingPhaseTest
    {
        [Fact]
        public void Execute_AddsSyntaxTree()
        {
            // Arrange
            var phase = new DefaultRazorParsingPhase();
            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            phase.Execute(codeDocument);

            // Assert
            Assert.NotNull(codeDocument.GetSyntaxTree());
        }
    }
}
