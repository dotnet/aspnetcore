// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultRazorDiagnosticTest
    {
        [Fact]
        public void DefaultRazorDiagnostic_Ctor()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "error", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            // Act
            var diagnostic = new DefaultRazorDiagnostic(descriptor, span, new object[0]);

            // Assert
            Assert.Equal("RZ0000", diagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(span, diagnostic.Span);
        }

        [Fact]
        public void DefaultRazorDiagnostic_GetMessage()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "error", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic = new DefaultRazorDiagnostic(descriptor, span, new object[0]);

            // Act
            var result = diagnostic.GetMessage();

            // Assert
            Assert.Equal("error", result);
        }


        [Fact]
        public void DefaultRazorDiagnostic_GetMessage_WithArgs()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic = new DefaultRazorDiagnostic(descriptor, span, new[] { "error" });

            // Act
            var result = diagnostic.GetMessage();

            // Assert
            Assert.Equal("this is an error", result);
        }

        [Fact]
        public void DefaultRazorDiagnostic_GetMessage_WithArgs_FormatProvider()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic = new DefaultRazorDiagnostic(descriptor, span, new object[] { 1.3m });

            // Act
            var result = diagnostic.GetMessage(CultureInfo.GetCultureInfo("fr-FR"));

            // Assert
            Assert.Equal("this is an 1,3", result);
        }


        [Fact]
        public void DefaultRazorDiagnostic_ToString()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "this is an error", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic = new DefaultRazorDiagnostic(descriptor, span, new object[0]);

            // Act
            var result = diagnostic.ToString();

            // Assert
            Assert.Equal("test.cs(2,9): Error RZ0000: this is an error", result);
        }

        [Fact]
        public void DefaultRazorDiagnostic_ToString_FormatProvider()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic = new DefaultRazorDiagnostic(descriptor, span, new object[] { 1.3m });

            // Act
            var result = ((IFormattable)diagnostic).ToString("ignored", CultureInfo.GetCultureInfo("fr-FR"));

            // Assert
            Assert.Equal("test.cs(2,9): Error RZ0000: this is an 1,3", result);
        }

        [Fact]
        public void DefaultRazorDiagnostic_Equals()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic1 = new DefaultRazorDiagnostic(descriptor, span, new object[0]);
            var diagnostic2 = new DefaultRazorDiagnostic(descriptor, span, new object[0]);

            // Act
            var result = diagnostic1.Equals(diagnostic2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DefaultRazorDiagnostic_NotEquals_DifferentLocation()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span1 = new SourceSpan("test.cs", 15, 1, 8, 5);
            var span2 = new SourceSpan("test.cs", 15, 1, 8, 3);

            var diagnostic1 = new DefaultRazorDiagnostic(descriptor, span1, new object[0]);
            var diagnostic2 = new DefaultRazorDiagnostic(descriptor, span2, new object[0]);

            // Act
            var result = diagnostic1.Equals(diagnostic2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DefaultRazorDiagnostic_NotEquals_DifferentId()
        {
            // Arrange
            var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var descriptor2 = new RazorDiagnosticDescriptor("RZ0002", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic1 = new DefaultRazorDiagnostic(descriptor1, span, new object[0]);
            var diagnostic2 = new DefaultRazorDiagnostic(descriptor2, span, new object[0]);

            // Act
            var result = diagnostic1.Equals(diagnostic2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DefaultRazorDiagnostic_HashCodesEqual()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic1 = new DefaultRazorDiagnostic(descriptor, span, new object[0]);
            var diagnostic2 = new DefaultRazorDiagnostic(descriptor, span, new object[0]);

            // Act
            var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DefaultRazorDiagnostic_HashCodesNotEqual_DifferentLocation()
        {
            // Arrange
            var descriptor = new RazorDiagnosticDescriptor("RZ0000", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span1 = new SourceSpan("test.cs", 15, 1, 8, 5);
            var span2 = new SourceSpan("test.cs", 15, 1, 8, 3);

            var diagnostic1 = new DefaultRazorDiagnostic(descriptor, span1, new object[0]);
            var diagnostic2 = new DefaultRazorDiagnostic(descriptor, span2, new object[0]);

            // Act
            var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DefaultRazorDiagnostic_HashCodesNotEqual_DifferentId()
        {
            // Arrange
            var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var descriptor2 = new RazorDiagnosticDescriptor("RZ0002", () => "this is an {0}", RazorDiagnosticSeverity.Error);
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);

            var diagnostic1 = new DefaultRazorDiagnostic(descriptor1, span, new object[0]);
            var diagnostic2 = new DefaultRazorDiagnostic(descriptor2, span, new object[0]);

            // Act
            var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

            // Assert
            Assert.False(result);
        }
    }
}
