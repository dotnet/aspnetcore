// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class LegacyRazorDiagnosticTest
    {
        [Fact]
        public void LegacyRazorDiagnostic_Ctor()
        {
            // Arrange
            var span = new SourceSpan("test.cs", 15, 1, 8, 5);
            var error = new RazorError("This is an error", new SourceLocation("test.cs", 15, 1, 8), 5);

            // Act
            var diagnostic = new LegacyRazorDiagnostic(error);

            // Assert
            Assert.Equal("RZ9999", diagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(span, diagnostic.Span);
        }

        [Fact]
        public void LegacyRazorDiagnostic_GetMessage()
        {
            // Arrange
            var error = new RazorError("This is an error", SourceLocation.Zero, 5);
            var diagnostic = new LegacyRazorDiagnostic(error);

            // Act
            var result = diagnostic.GetMessage();

            // Assert
            Assert.Equal("This is an error", result);
        }

        // RazorError doesn't support format strings.
        [Fact]
        public void LegacyRazorDiagnostic_GetMessage_FormatProvider()
        {
            // Arrange
            var error = new RazorError("This is an error", SourceLocation.Zero, 5);
            var diagnostic = new LegacyRazorDiagnostic(error);

            // Act
            var result = diagnostic.GetMessage(CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal("This is an error", result);
        }

        [Fact]
        public void LegacyRazorDiagnostic_ToString()
        {
            // Arrange
            var error = new RazorError("This is an error", SourceLocation.Zero, 5);
            var diagnostic = new LegacyRazorDiagnostic(error);

            // Act
            var result = diagnostic.ToString();

            // Assert
            Assert.Equal("(1,1): Error RZ9999: This is an error", result);
        }

        [Fact]
        public void LegacyRazorDiagnostic_ToString_FormatProvider()
        {
            // Arrange
            var error = new RazorError("This is an error", SourceLocation.Zero, 5);
            var diagnostic = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));

            // Act
            var result = ((IFormattable)diagnostic).ToString("ignored", CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal("(1,1): Error RZ9999: This is an error", result);
        }

        [Fact]
        public void LegacyRazorDiagnostic_Equals()
        {
            // Arrange
            var diagnostic1 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));
            var diagnostic2 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));

            // Act
            var result = diagnostic1.Equals(diagnostic2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void LegacyRazorDiagnostic_NotEquals_DifferentLocation()
        {
            // Arrange
            var diagnostic1 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));
            var diagnostic2 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 1));

            // Act
            var result = diagnostic1.Equals(diagnostic2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LegacyRazorDiagnostic_NotEquals_DifferentMessage()
        {
            // Arrange
            var diagnostic1 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));
            var diagnostic2 = new LegacyRazorDiagnostic(new RazorError("This is maybe an error", SourceLocation.Zero, 5));

            // Act
            var result = diagnostic1.Equals(diagnostic2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LegacyRazorDiagnostic_HashCodesEqual()
        {
            // Arrange
            var diagnostic1 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));
            var diagnostic2 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));

            // Act
            var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void LegacyRazorDiagnostic_HashCodesNotEqual_DifferentLocation()
        {
            // Arrange
            var diagnostic1 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));
            var diagnostic2 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 2));

            // Act
            var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LegacyRazorDiagnostic_HashCodesNotEqual_DifferentMessage()
        {
            // Arrange
            var diagnostic1 = new LegacyRazorDiagnostic(new RazorError("This is an error", SourceLocation.Zero, 5));
            var diagnostic2 = new LegacyRazorDiagnostic(new RazorError("This is maybe an error", SourceLocation.Zero, 5));

            // Act
            var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

            // Assert
            Assert.False(result);
        }
    }
}
