// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class DefaultExtensionDependencyCheckerTest
    {
        [Fact]
        public void Check_ReturnsFalse_WithMissingDependency()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var output = new StringWriter();

                var alphaFilePath = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha.dll");

                var loader = new TestDefaultExtensionAssemblyLoader(Path.Combine(directory.DirectoryPath, "shadow"));
                var checker = new DefaultExtensionDependencyChecker(loader, output);

                // Act
                var result = checker.Check(new[] { alphaFilePath, });

                // Assert
                Assert.False(result, "Check should not have passed: " + output.ToString());
            }
        }

        [Fact]
        public void Check_ReturnsTrue_WithAllDependenciesProvided()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var output = new StringWriter();

                var alphaFilePath = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha.dll");
                var betaFilePath = LoaderTestResources.Beta.WriteToFile(directory.DirectoryPath, "Beta.dll");
                var gammaFilePath = LoaderTestResources.Gamma.WriteToFile(directory.DirectoryPath, "Gamma.dll");
                var deltaFilePath = LoaderTestResources.Delta.WriteToFile(directory.DirectoryPath, "Delta.dll");

                var loader = new TestDefaultExtensionAssemblyLoader(Path.Combine(directory.DirectoryPath, "shadow"));
                var checker = new DefaultExtensionDependencyChecker(loader, output);

                // Act
                var result = checker.Check(new[] { alphaFilePath, betaFilePath, gammaFilePath, deltaFilePath, });

                // Assert
                Assert.True(result, "Check should have passed: " + output.ToString());
            }
        }

        [Fact]
        public void Check_ReturnsFalse_WhenAssemblyHasDifferentMVID()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var output = new StringWriter();

                // Load Beta.dll from the future Alpha.dll path to prime the assembly loader
                var alphaFilePath = LoaderTestResources.Beta.WriteToFile(directory.DirectoryPath, "Alpha.dll");
                var betaFilePath = LoaderTestResources.Beta.WriteToFile(directory.DirectoryPath, "Beta.dll");
                var gammaFilePath = LoaderTestResources.Gamma.WriteToFile(directory.DirectoryPath, "Gamma.dll");
                var deltaFilePath = LoaderTestResources.Delta.WriteToFile(directory.DirectoryPath, "Delta.dll");

                var loader = new TestDefaultExtensionAssemblyLoader(Path.Combine(directory.DirectoryPath, "shadow"));
                var checker = new DefaultExtensionDependencyChecker(loader, output);

                // This will cause the loader to cache some inconsistent information.
                loader.LoadFromPath(alphaFilePath);
                LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha.dll");

                // Act
                var result = checker.Check(new[] { alphaFilePath, gammaFilePath, deltaFilePath, });

                // Assert
                Assert.False(result, "Check should not have passed: " + output.ToString());
            }
        }

        [Fact]
        public void Check_ReturnsFalse_WhenLoaderThrows()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var output = new StringWriter();
                
                var deltaFilePath = LoaderTestResources.Delta.WriteToFile(directory.DirectoryPath, "Delta.dll");

                var loader = new Mock<ExtensionAssemblyLoader>();
                loader
                    .Setup(l => l.LoadFromPath(It.IsAny<string>()))
                    .Throws(new InvalidOperationException());
                var checker = new DefaultExtensionDependencyChecker(loader.Object, output);

                // Act
                var result = checker.Check(new[] { deltaFilePath, });

                // Assert
                Assert.False(result, "Check should not have passed: " + output.ToString());
            }
        }
    }
}