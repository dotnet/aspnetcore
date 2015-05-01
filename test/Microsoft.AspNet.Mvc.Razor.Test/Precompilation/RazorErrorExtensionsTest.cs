// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    public class RazorErrorExtensionsTest
    {
        public static TheoryData ToDiagnostic_SucceedsWhenRazorErrorLocationIsZeroOrUndefinedData
        {
            get
            {
                return new TheoryData<SourceLocation, int>
                {
                    { SourceLocation.Undefined, -1 },
                    { SourceLocation.Undefined, 0 },
                    { SourceLocation.Zero, -1 },
                    { SourceLocation.Zero, 0 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ToDiagnostic_SucceedsWhenRazorErrorLocationIsZeroOrUndefinedData))]
        public void ToDiagnostic_SucceedsWhenRazorErrorLocationIsZeroOrUndefined(
            SourceLocation location,
            int length)
        {
            // Arrange
            var error = new RazorError("some message", location, length);

            // Act
            var diagnostics = error.ToDiagnostics("/some-path");

            // Assert
            var span = diagnostics.Location.GetMappedLineSpan();
            Assert.Equal("/some-path", span.Path);
            Assert.Equal(0, span.StartLinePosition.Line);
            Assert.Equal(0, span.StartLinePosition.Character);
            Assert.Equal(0, span.EndLinePosition.Line);
            Assert.Equal(0, span.EndLinePosition.Character);
        }

        [Fact]
        public void ToDiagnostic_ConvertsRazorErrorLocation_ToSourceLineMappings()
        {
            // Arrange
            var sourceLocation = new SourceLocation(absoluteIndex: 30, lineIndex: 10, characterIndex: 1);
            var error = new RazorError("some message", sourceLocation, length: 5);

            // Act
            var diagnostics = error.ToDiagnostics("/some-path");

            // Assert
            var span = diagnostics.Location.GetMappedLineSpan();
            Assert.Equal("/some-path", span.Path);
            Assert.Equal(10, span.StartLinePosition.Line);
            Assert.Equal(1, span.StartLinePosition.Character);
            Assert.Equal(10, span.EndLinePosition.Line);
            Assert.Equal(6, span.EndLinePosition.Character);
        }
    }
}