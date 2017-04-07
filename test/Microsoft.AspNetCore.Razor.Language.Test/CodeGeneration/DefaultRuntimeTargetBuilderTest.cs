// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
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

            var extensions = new IRuntimeTargetExtension[]
            {
                new MyExtension1(),
                new MyExtension2(),
            };
            
            for (var i = 0; i < extensions.Length; i++)
            {
                builder.TargetExtensions.Add(extensions[i]);
            }

            // Act
            var result = builder.Build();

            // Assert
            var target = Assert.IsType<DefaultRuntimeTarget>(result);
            Assert.Equal(extensions, target.Extensions);
        }

        private class MyExtension1 : IRuntimeTargetExtension
        {
        }

        private class MyExtension2 : IRuntimeTargetExtension
        {
        }
    }
}
