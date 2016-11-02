// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Utilities;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace NuGetPackager
{
    internal class PackCommand
    {
        private readonly string _baseDir;

        public PackCommand(string baseDir)
        {
            _baseDir = baseDir;
        }

        public async Task PackAsync(string nuspec, string config, string outputDir)
        {
            var project = ProjectContext.Create(Path.GetDirectoryName(nuspec), FrameworkConstants.CommonFrameworks.NetCoreApp10);
            var idx = 0;
            var props = "";
            var first = false;
            foreach (var depVersion in GetDependencies(project).OrderBy(p => p.Item1).Select(p => p.Item2))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    props += ";";
                }

                props += $"dep_{++idx}={depVersion}";
            }
            var publishDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts/build", project.ProjectFile.Name);
            if (Directory.Exists(publishDir))
            {
                Directory.Delete(publishDir, recursive: true);
            }
            Directory.CreateDirectory(publishDir);

            var buildCommand = Command.CreateDotNet("publish",
                new[] { project.ProjectFile.ProjectFilePath, "--configuration", config, "--output", publishDir },
                 configuration: config);

            if (buildCommand.Execute().ExitCode != 0)
            {
                throw new GracefulException("Build failed");
            }

            Directory.CreateDirectory(outputDir);

            var version = project.ProjectFile.Version.ToNormalizedString();
            await Nuget("pack",
                nuspec,
                "-Verbosity", "detailed",
                "-OutputDirectory", outputDir,
                "-Version", version,
                "-Properties", props,
                "-BasePath", publishDir);
        }

        private IEnumerable<Tuple<string, string>> GetDependencies(ProjectContext context)
        {
            // copied from https://github.com/dotnet/cli/blob/da0e365264e0ab555cdde978bdfd2e504bada49a/src/dotnet/commands/dotnet-pack/PackageGenerator.cs
            var project = context.RootProject;

            foreach (var dependency in project.Dependencies)
            {
                if (dependency.Type.Equals(LibraryDependencyType.Build))
                {
                    continue;
                }

                // TODO: Efficiency
                var dependencyDescription = context.LibraryManager.GetLibraries().First(l => l.RequestedRanges.Contains(dependency));

                // REVIEW: Can we get this far with unresolved dependencies
                if (!dependencyDescription.Resolved)
                {
                    continue;
                }

                if (dependencyDescription.Identity.Type == LibraryType.Project &&
                    ((ProjectDescription)dependencyDescription).Project.EmbedInteropTypes)
                {
                    continue;
                }

                VersionRange dependencyVersion = null;

                if (dependency.VersionRange == null ||
                    dependency.VersionRange.IsFloating)
                {
                    dependencyVersion = new VersionRange(dependencyDescription.Identity.Version);
                }
                else
                {
                    dependencyVersion = dependency.VersionRange;
                }

                Reporter.Verbose.WriteLine($"Adding dependency {dependency.Name.Yellow()} {VersionUtility.RenderVersion(dependencyVersion).Yellow()}");

                yield return new Tuple<string, string>(dependency.Name, dependencyVersion.MinVersion.ToString());
            }
        }

        private static string GetLockFileVersion(ProjectContext project, string name) =>
            project
                .LockFile
                .PackageLibraries
                .First(l => l.Name.Equals(name))
                .Version
                .ToNormalizedString();

        private async Task Nuget(params string[] args)
        {
            var pInfo = new ProcessStartInfo
            {
                Arguments = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args),
                FileName = await GetNugetExePath()
            };
            Console.WriteLine("command:   ".Bold().Blue() + pInfo.FileName);
            Console.WriteLine("arguments: ".Bold().Blue() + pInfo.Arguments);

            Process.Start(pInfo).WaitForExit();
        }

        private async Task<string> GetNugetExePath()
        {
            if (Environment.GetEnvironmentVariable("KOREBUILD_NUGET_EXE") != null)
            {
                return Environment.GetEnvironmentVariable("KOREBUILD_NUGET_EXE");
            }

            var nugetPath = Path.Combine(_baseDir, ".build", "nuget.3.5.0-rc1.exe");
            if (File.Exists(nugetPath))
            {
                return nugetPath;
            }

            Console.WriteLine("log : Downloading nuget.exe 3.5.0-rc1".Bold().Black());

            var response = await new HttpClient().GetAsync("https://dist.nuget.org/win-x86-commandline/v3.5.0-rc1/NuGet.exe");
            using (var file = new FileStream(nugetPath, FileMode.CreateNew))
            {
                response.EnsureSuccessStatusCode();
                await response.Content.LoadIntoBufferAsync();
                await response.Content.CopyToAsync(file);
            }
            return nugetPath;
        }
    }
}