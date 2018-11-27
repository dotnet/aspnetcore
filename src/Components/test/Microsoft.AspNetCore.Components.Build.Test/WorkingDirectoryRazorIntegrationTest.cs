// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    // Integration tests focused on file path handling for class/namespace names
    public class WorkingDirectoryRazorIntegrationTest : RazorIntegrationTestBase
    {
        public WorkingDirectoryRazorIntegrationTest()
        {
            WorkingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ArbitraryWindowsPath : ArbitraryMacLinuxPath;
            WorkingDirectory += "-Dir";
        }

        internal override string WorkingDirectory { get; }

        [Theory]
        [InlineData("ItemAtRoot.cs", "Test_Dir", "ItemAtRoot")]
        [InlineData("Dir1\\MyFile.cs", "Test_Dir.Dir1", "MyFile")]
        [InlineData("Dir1\\Dir2\\MyFile.cs", "Test_Dir.Dir1.Dir2", "MyFile")]
        public void CreatesClassWithCorrectNameAndNamespace(string relativePath, string expectedNamespace, string expectedClassName)
        {
            // Arrange
            relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);

            // Act
            var result = CompileToAssembly(relativePath, "");

            // Assert
            Assert.Empty(result.Diagnostics);

            var type = Assert.Single(result.Assembly.GetTypes());
            Assert.Equal(expectedNamespace, type.Namespace);
            Assert.Equal(expectedClassName, type.Name);
        }
    }
}
