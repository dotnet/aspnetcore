// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DefaultCodeTargetBuilderTest
    {
        [Fact]
        public void Build_CreatesDefaultCodeTarget()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            var builder = new DefaultCodeTargetBuilder(codeDocument, options);

            var extensions = new ICodeTargetExtension[]
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
            var target = Assert.IsType<DefaultCodeTarget>(result);
            Assert.Equal(extensions, target.Extensions);
        }

        private class MyExtension1 : ICodeTargetExtension
        {
        }

        private class MyExtension2 : ICodeTargetExtension
        {
        }
    }
}
