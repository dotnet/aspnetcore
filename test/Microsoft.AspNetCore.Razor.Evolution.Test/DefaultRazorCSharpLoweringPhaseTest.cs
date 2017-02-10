// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.AspNetCore.Razor.Evolution.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultRazorCSharpLoweringPhaseTest
    {
        [Fact]
        public void Execute_ThrowsForMissingDependency_IRDocument()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();

            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The '{nameof(DefaultRazorCSharpLoweringPhase)}' phase requires a '{nameof(DocumentIRNode)}' " +
                $"provided by the '{nameof(RazorCodeDocument)}'.");
        }

        [Fact]
        public void Execute_ThrowsForMissingDependency_SyntaxTree()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();

            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var irDocument = new DocumentIRNode();
            codeDocument.SetIRDocument(irDocument);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The '{nameof(DefaultRazorCSharpLoweringPhase)}' phase requires a '{nameof(RazorSyntaxTree)}' " +
                $"provided by the '{nameof(RazorCodeDocument)}'.");
        }

        [Fact]
        public void Execute_ThrowsForMissingDependency_RuntimeTarget()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();

            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));

            var irDocument = new DocumentIRNode()
            {
                DocumentKind = "test",
            };
            codeDocument.SetIRDocument(irDocument);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The document of kind 'test' does not have a '{nameof(RuntimeTarget)}'. " +
                $"The document classifier must set a value for '{nameof(DocumentIRNode.Target)}'.");
        }
    }
}
