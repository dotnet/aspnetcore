// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorDiagnosticTest
    {
        [Fact]
        public void Create_WithDescriptor_CreatesDefaultRazorDiagnostic()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0001", () => "a", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            // Act
            var diagnostic = RazorDiagnostic.Create(descriptor, span);

            // Assert
            var defaultDiagnostic = Assert.IsType<DefaultRazorDiagnostic>(diagnostic);
            Assert.Equal("RZ0001", defaultDiagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Error, defaultDiagnostic.Severity);
            Assert.Equal(span, diagnostic.Span);
        }

        [Fact]
        public void Create_WithDescriptor_AndArgs_CreatesDefaultRazorDiagnostic()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0001", () => "a", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            // Act
            var diagnostic = RazorDiagnostic.Create(descriptor, span, "Hello", "World");

            // Assert
            var defaultDiagnostic = Assert.IsType<DefaultRazorDiagnostic>(diagnostic);
            Assert.Equal("RZ0001", defaultDiagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Error, defaultDiagnostic.Severity);
            Assert.Equal(span, diagnostic.Span);
        }

        [Fact]
        public void Create_WithRazorError_CreatesLegacyRazorDiagnostic()
        {
            // Arrange
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);
            var error = new RazorError("This is an error", new SourceLocation("test.cs", 15, 1, 8), 5);

            // Act
            var diagnostic = RazorDiagnostic.Create(error);

            // Assert
            var legacyDiagnostic = Assert.IsType<LegacyRazorDiagnostic>(diagnostic);
            Assert.Equal("RZ9999", legacyDiagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Error, legacyDiagnostic.Severity);
            Assert.Equal(span, diagnostic.Span);
        }
    }
}
