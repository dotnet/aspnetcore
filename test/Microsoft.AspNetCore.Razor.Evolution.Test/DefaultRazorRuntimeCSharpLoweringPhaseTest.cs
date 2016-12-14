// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultRazorRuntimeCSharpLoweringPhaseTest
    {
        [Fact]
        public void Execute_ThrowsForMissingDependency()
        {
            // Arrange
            var phase = new DefaultRazorRuntimeCSharpLoweringPhase();

            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The '{nameof(DefaultRazorRuntimeCSharpLoweringPhase)}' phase requires a '{nameof(DocumentIRNode)}' " +
                $"provided by the '{nameof(RazorCodeDocument)}'.");
        }
    }
}
