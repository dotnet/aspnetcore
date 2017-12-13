// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal class ProjectDirectory : IDisposable
    {
        public static ProjectDirectory Create(string projectName)
        {
            var destinationPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(destinationPath);

            try
            {
                if (Directory.EnumerateFiles(destinationPath).Any())
                {
                    throw new InvalidOperationException($"{destinationPath} should be empty");
                }

                var solutionRoot = TestPathUtilities.GetSolutionRootDirectory("Razor");
                if (solutionRoot == null)
                {
                    throw new InvalidOperationException("Could not find solution root.");
                }

                var projectRoot = Path.Combine(solutionRoot, "test", "testapps", projectName);
                if (!Directory.Exists(projectRoot))
                {
                    throw new InvalidOperationException($"Could not find project at '{projectRoot}'");
                }

                CopyDirectory(new DirectoryInfo(projectRoot), new DirectoryInfo(destinationPath));

                foreach (var project in Directory.EnumerateFiles(destinationPath, "*.csproj"))
                {
                    RewriteCsproj(projectRoot, project);
                }

                return new ProjectDirectory(destinationPath);
            }
            catch
            {
                CleanupDirectory(destinationPath);
                throw;
            }

            void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
            {
                foreach (var directory in source.EnumerateDirectories())
                {
                    if (directory.Name == "bin" || directory.Name == "obj")
                    {
                        // Just in case someone has opened the project in an IDE or built it. We don't want to copy
                        // these folders.
                        continue;
                    }

                    var created = destination.CreateSubdirectory(directory.Name);
                    CopyDirectory(directory, created);
                }

                foreach (var file in source.EnumerateFiles())
                {
                    file.CopyTo(Path.Combine(destination.FullName, file.Name));
                }
            }

            void RewriteCsproj(string originalProjectRoot, string filePath)
            {
                // We need to replace $(OriginalProjectRoot) with the path to the original directory
                // that way relative references will resolve.
                var text = File.ReadAllText(filePath);
                text = text.Replace("$(OriginalProjectRoot)", originalProjectRoot);
                File.WriteAllText(filePath, text);
            }
        }

        private ProjectDirectory(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            CleanupDirectory(DirectoryPath);
        }

        private static void CleanupDirectory(string filePath)
        {
            var tries = 5;
            var sleep = TimeSpan.FromSeconds(3);

            for (var i = 0; i < tries; i++)
            {
                try
                {
                    Directory.Delete(filePath, recursive: true);
                    return;
                }
                catch when (i < tries - 1)
                {
                    Console.WriteLine($"Failed to delete directory {filePath}, trying again.");
                    Thread.Sleep(sleep);
                }
            }
        }
    }
}
