// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class CodeTargetTest
    {
        [Fact]
        public void CreateDefault_CreatesDefaultCodeTarget()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            // Act
            var target = CodeTarget.CreateDefault(codeDocument, options);

            // Assert
            Assert.IsType<DefaultCodeTarget>(target);
        }

        [Fact]
        public void CreateDefault_CallsDelegate()
        {
            // Arrange
            var wasCalled = false;
            Action<CodeTargetBuilder> @delegate = (b) => { wasCalled = true; };

            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            // Act
            CodeTarget.CreateDefault(codeDocument, options, @delegate);

            // Assert
            Assert.True(wasCalled);
        }

        [Fact]
        public void CreateDefault_AllowsNullDelegate()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            // Act
            CodeTarget.CreateDefault(codeDocument, options, configure: null);

            // Assert (does not throw)
        }

        [Fact]
        public void CreateEmpty_AllowsNullDelegate()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            // Act
            CodeTarget.CreateDefault(codeDocument, options, configure: null);

            // Assert (does not throw)
        }
    }
}
