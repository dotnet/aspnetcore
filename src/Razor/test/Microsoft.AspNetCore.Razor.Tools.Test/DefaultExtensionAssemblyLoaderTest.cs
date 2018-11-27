// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class DefaultExtensionAssemblyLoaderTest
    {
        [Fact]
        public void LoadFromPath_CanLoadAssembly()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var alphaFilePath = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha.dll");

                var loader = new TestDefaultExtensionAssemblyLoader(Path.Combine(directory.DirectoryPath, "shadow"));

                // Act
                var assembly = loader.LoadFromPath(alphaFilePath);

                // Assert
                Assert.NotNull(assembly);
            }
        }

        [Fact]
        public void LoadFromPath_DoesNotAddDuplicates_AfterLoadingByName()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var alphaFilePath = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha.dll");
                var alphaFilePath2 = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha2.dll");

                var loader = new TestDefaultExtensionAssemblyLoader(Path.Combine(directory.DirectoryPath, "shadow"));
                loader.AddAssemblyLocation(alphaFilePath);

                var assembly1 = loader.Load("Alpha");

                // Act
                var assembly2 = loader.LoadFromPath(alphaFilePath2);

                // Assert
                Assert.Same(assembly1, assembly2);
            }
        }

        [Fact]
        public void LoadFromPath_DoesNotAddDuplicates_AfterLoadingByPath()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var alphaFilePath = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha.dll");
                var alphaFilePath2 = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha2.dll");

                var loader = new TestDefaultExtensionAssemblyLoader(Path.Combine(directory.DirectoryPath, "shadow"));
                var assembly1 = loader.LoadFromPath(alphaFilePath);

                // Act
                var assembly2 = loader.LoadFromPath(alphaFilePath2);

                // Assert
                Assert.Same(assembly1, assembly2);
            }
        }

        [Fact]
        public void Load_CanLoadAssemblyByName_AfterLoadingByPath()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var alphaFilePath = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha.dll");

                var loader = new TestDefaultExtensionAssemblyLoader(Path.Combine(directory.DirectoryPath, "shadow"));
                var assembly1 = loader.LoadFromPath(alphaFilePath);

                // Act
                var assembly2 = loader.Load(assembly1.FullName);

                // Assert
                Assert.Same(assembly1, assembly2);
            }
        }

        [Fact]
        public void LoadFromPath_WithDependencyPathsSpecified_CanLoadAssemblyDependencies()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var alphaFilePath = LoaderTestResources.Alpha.WriteToFile(directory.DirectoryPath, "Alpha.dll");
                var betaFilePath = LoaderTestResources.Beta.WriteToFile(directory.DirectoryPath, "Beta.dll");
                var gammaFilePath = LoaderTestResources.Gamma.WriteToFile(directory.DirectoryPath, "Gamma.dll");
                var deltaFilePath = LoaderTestResources.Delta.WriteToFile(directory.DirectoryPath, "Delta.dll");

                var loader = new TestDefaultExtensionAssemblyLoader(Path.Combine(directory.DirectoryPath, "shadow"));
                loader.AddAssemblyLocation(gammaFilePath);
                loader.AddAssemblyLocation(deltaFilePath);

                // Act
                var alpha = loader.LoadFromPath(alphaFilePath);
                var beta = loader.LoadFromPath(betaFilePath);

                // Assert
                var builder = new StringBuilder();

                var a = alpha.CreateInstance("Alpha.A");
                a.GetType().GetMethod("Write").Invoke(a, new object[] { builder, "Test A" });

                var b = beta.CreateInstance("Beta.B");
                b.GetType().GetMethod("Write").Invoke(b, new object[] { builder, "Test B" });
                var expected = @"Delta: Gamma: Alpha: Test A
Delta: Gamma: Beta: Test B
";

                var actual = builder.ToString();

                Assert.Equal(expected, actual);
            }
        }
    }
}