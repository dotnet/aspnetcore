// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MonoDevelop.Core;
using MonoDevelop.Projects;
using Xunit;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    public class FallbackRazorProjectHostTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsMvcAssembly_FailsIfNullOrEmptyFilePath(string filePath)
        {
            // Arrange
            var assemblyFilePath = new FilePath(filePath);
            var assemblyReference = new AssemblyReference(assemblyFilePath);

            // Act
            var result = FallbackRazorProjectHost.IsMvcAssembly(assemblyReference);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMvcAssembly_FailsIfNotMvc()
        {
            // Arrange
            var assemblyFilePath = new FilePath("C:/Path/To/Assembly.dll");
            var assemblyReference = new AssemblyReference(assemblyFilePath);

            // Act
            var result = FallbackRazorProjectHost.IsMvcAssembly(assemblyReference);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMvcAssembly_SucceedsIfMvc()
        {
            // Arrange
            var assemblyFilePath = new FilePath("C:/Path/To/Microsoft.AspNetCore.Mvc.Razor.dll");
            var assemblyReference = new AssemblyReference(assemblyFilePath);

            // Act
            var result = FallbackRazorProjectHost.IsMvcAssembly(assemblyReference);

            // Assert
            Assert.True(result);
        }

        // -------------------------------------------------------------------------------------------
        // Purposefully do not have any more tests here because that would involve mocking MonoDevelop 
        // types. The default constructors for the Solution / DotNetProject MonoDevelop types change
        // static classes (they assume they're being created in an IDE).
        // -------------------------------------------------------------------------------------------
    }
}
