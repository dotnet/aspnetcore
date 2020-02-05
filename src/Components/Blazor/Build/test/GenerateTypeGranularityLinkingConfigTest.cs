// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Blazor.Build.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class GenerateTypeGranularityLinkingConfigTest : IDisposable
    {
        private readonly DirectoryInfo TempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "blazor-granular"));

        [Fact]
        public void Execute_WritesFilesToDisk()
        {
            //  Arrange
            var assemblyNames = new[]
            {
                "Microsoft.Extensions.Configuration",
                "Microsoft.Extensions.Logging",
            };

            var assemblies = assemblyNames.Select(name => new TaskItem($"{name}.dll")).ToArray();
            var outputPath = Path.Combine(TempDir.FullName, Path.GetRandomFileName());
            var task = new GenerateTypeGranularityLinkingConfig
            {
                BuildEngine = Mock.Of<IBuildEngine>(),
                Assemblies = assemblies,
                OutputPath = outputPath,
            };

            // Act
            task.Execute();

            // Assert
            Assert.True(File.Exists(outputPath), $"No file found at {outputPath}.");
            var xml = XDocument.Load(outputPath).Root;

            for (var i = 0; i < assemblyNames.Length; i++)
            {
                Assert.Equal(assemblyNames[i], xml.Elements("assembly").ElementAt(i).Attribute("fullname").Value);
            }
        }

        [Fact]
        public void Execute_DoesNotWriteFileToDisk_IfContentsAreIdentical()
        {
            //  Arrange
            var assemblyNames = new[]
            {
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Configuration",
            };

            var assemblies = assemblyNames.Select(name => new TaskItem($"{name}.dll")).ToArray();
            var outputPath = Path.Combine(TempDir.FullName, Path.GetRandomFileName());
            var task = new GenerateTypeGranularityLinkingConfig
            {
                BuildEngine = Mock.Of<IBuildEngine>(),
                Assemblies = assemblies,
                OutputPath = outputPath,
            };
            task.Execute();
            var thumbPrint = FileThumbPrint.Create(outputPath);

            // Act
            task.Execute();
            Assert.Equal(thumbPrint, FileThumbPrint.Create(outputPath));
        }

        [Fact]
        public void Execute_UpdatesFile()
        {
            //  Arrange
            var assemblyNames = new[]
            {
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Configuration",
            };

            var assemblies = assemblyNames.Select(name => new TaskItem($"{name}.dll")).ToArray();
            var outputPath = Path.Combine(TempDir.FullName, Path.GetRandomFileName());
            var task = new GenerateTypeGranularityLinkingConfig
            {
                BuildEngine = Mock.Of<IBuildEngine>(),
                Assemblies = assemblies,
                OutputPath = outputPath,
            };
            task.Execute();
            var thumbPrint = FileThumbPrint.Create(outputPath);

            assemblyNames = new[]
            {
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Configuration",
                "Microsoft.AspNetCore.SignalR.Client",
            };
            task.Assemblies = assemblyNames.Select(name => new TaskItem($"{name}.dll")).ToArray();

            // Act
            task.Execute();

            // Assert
            Assert.NotEqual(thumbPrint, FileThumbPrint.Create(outputPath));
            var xml = XDocument.Load(outputPath).Root;
            for (var i = 0; i < assemblyNames.Length; i++)
            {
                Assert.Equal(assemblyNames[i], xml.Elements("assembly").ElementAt(i).Attribute("fullname").Value);
            }
        }

        public void Dispose()
        {
            try
            {
                TempDir.Delete();
            }
            catch
            {
                // Don't fail
            }
        }
    }
}
