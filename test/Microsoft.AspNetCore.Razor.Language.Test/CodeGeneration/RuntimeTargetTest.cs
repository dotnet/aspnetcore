// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RuntimeTargetTest
    {
        [Fact]
        public void CreateDefault_CreatesDefaultRuntimeTarget()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorParserOptions.CreateDefaultOptions();

            // Act
            var target = RuntimeTarget.CreateDefault(codeDocument, options);

            // Assert
            Assert.IsType<DefaultRuntimeTarget>(target);
        }

        [Fact]
        public void CreateDefault_CallsDelegate()
        {
            // Arrange
            var wasCalled = false;
            Action<IRuntimeTargetBuilder> @delegate = (b) => { wasCalled = true; };

            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorParserOptions.CreateDefaultOptions();

            // Act
            RuntimeTarget.CreateDefault(codeDocument, options, @delegate);

            // Assert
            Assert.True(wasCalled);
        }

        [Fact]
        public void CreateDefault_AllowsNullDelegate()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorParserOptions.CreateDefaultOptions();

            // Act
            RuntimeTarget.CreateDefault(codeDocument, options, configure: null);

            // Assert (does not throw)
        }

        [Fact]
        public void CreateEmpty_AllowsNullDelegate()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorParserOptions.CreateDefaultOptions();

            // Act
            RuntimeTarget.CreateDefault(codeDocument, options, configure: null);

            // Assert (does not throw)
        }
    }
}
