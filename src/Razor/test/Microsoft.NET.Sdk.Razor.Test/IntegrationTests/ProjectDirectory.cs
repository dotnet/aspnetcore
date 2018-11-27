// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal class ProjectDirectory : IDisposable
    {
#if PRESERVE_WORKING_DIRECTORY
        public bool PreserveWorkingDirectory { get; set; } = true;
#else
        public bool PreserveWorkingDirectory { get; set; }
#endif

        public static ProjectDirectory Create(string originalProjectName, string targetProjectName, string baseDirectory, string[] additionalProjects, string language)
        {
            var destinationPath = Path.Combine(Path.GetTempPath(), "Razor", baseDirectory, Path.GetRandomFileName());
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

                var binariesRoot = Path.GetDirectoryName(typeof(ProjectDirectory).Assembly.Location);

                foreach (var project in new string[] { originalProjectName, }.Concat(additionalProjects))
                {
                    var testAppsRoot = Path.Combine(solutionRoot, "test", "testapps");
                    var projectRoot = Path.Combine(testAppsRoot, project);
                    if (!Directory.Exists(projectRoot))
                    {
                        throw new InvalidOperationException($"Could not find project at '{projectRoot}'");
                    }

                    var projectDestination = Path.Combine(destinationPath, project);
                    var projectDestinationDir = Directory.CreateDirectory(projectDestination);
                    CopyDirectory(new DirectoryInfo(projectRoot), projectDestinationDir);
                    SetupDirectoryBuildFiles(solutionRoot, binariesRoot, testAppsRoot, projectDestination);
                }

                // Rename the csproj/fsproj
                string extension;
                if (language.Equals("C#", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".csproj";
                }
                else if (language.Equals("F#", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".fsproj";
                }
                else
                {
                    throw new InvalidOperationException($"Language {language} is not supported.");
                }
                var directoryPath = Path.Combine(destinationPath, originalProjectName);
                var oldProjectFilePath = Path.Combine(directoryPath, originalProjectName + extension);
                var newProjectFilePath = Path.Combine(directoryPath, targetProjectName + extension);
                File.Move(oldProjectFilePath, newProjectFilePath);

                CopyGlobalJson(solutionRoot, destinationPath);

                return new ProjectDirectory(
                    destinationPath,
                    directoryPath,
                    newProjectFilePath);
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

            void SetupDirectoryBuildFiles(string solutionRoot, string binariesRoot, string testAppsRoot, string projectDestination)
            {
                var beforeDirectoryPropsContent =
$@"<Project>
  <PropertyGroup>
    <SolutionRoot>{solutionRoot}</SolutionRoot>
    <BinariesRoot>{binariesRoot}</BinariesRoot>
  </PropertyGroup>
</Project>";
                File.WriteAllText(Path.Combine(projectDestination, "Before.Directory.Build.props"), beforeDirectoryPropsContent);

                new List<string> { "Directory.Build.props", "Directory.Build.targets", "RazorTest.Introspection.targets" }
                    .ForEach(file =>
                    {
                        var source = Path.Combine(testAppsRoot, file);
                        var destination = Path.Combine(projectDestination, file);
                        File.Copy(source, destination);
                    });
            }

            void CopyGlobalJson(string solutionRoot, string projectRoot)
            {
                var srcGlobalJson = Path.Combine(solutionRoot, "global.json");
                if (!File.Exists(srcGlobalJson))
                {
                    throw new InvalidOperationException("global.json at the root of the repository could not be found. Run './build /t:Noop' at the repository root and re-run these tests.");
                }

                var destinationGlobalJson = Path.Combine(projectRoot, "global.json");
                File.Copy(srcGlobalJson, destinationGlobalJson);
            }
        }

        protected ProjectDirectory(string solutionPath, string directoryPath, string projectFilePath)
        {
            SolutionPath = solutionPath;
            DirectoryPath = directoryPath;
            ProjectFilePath = projectFilePath;
        }

        public string DirectoryPath { get; }

        public string ProjectFilePath { get;}

        public string SolutionPath { get; }

        public void Dispose()
        {
            if (PreserveWorkingDirectory)
            {
                Console.WriteLine($"Skipping deletion of working directory {SolutionPath}");
            }
            else
            {
                CleanupDirectory(SolutionPath);
            }
        }

        internal static void CleanupDirectory(string filePath)
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
