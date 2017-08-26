// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NuGet.ProjectModel;

namespace RepoTasks.VersionPinning
{
    public class DependencyGraphSpecProvider : IDisposable
    {
        private readonly string _packageSpecDirectory;
        private readonly bool _deleteSpecDirectoryOnDispose;
        private readonly string _dotnetPath;

        public DependencyGraphSpecProvider(string packageSpecDirectory)
            : this(packageSpecDirectory, deleteSpecDirectoryOnDispose: false)
        {
        }

        private DependencyGraphSpecProvider(string packageSpecDirectory, bool deleteSpecDirectoryOnDispose)
        {
            _packageSpecDirectory = packageSpecDirectory;
            _deleteSpecDirectoryOnDispose = deleteSpecDirectoryOnDispose;
            _dotnetPath = Process.GetCurrentProcess().MainModule.FileName;
        }

        public static DependencyGraphSpecProvider Default { get; } =
            new DependencyGraphSpecProvider(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), deleteSpecDirectoryOnDispose: true);

        public DependencyGraphSpec GetDependencyGraphSpec(string repositoryName, string solutionPath)
        {
            var outputFile = Path.Combine(_packageSpecDirectory, repositoryName, Path.GetFileName(solutionPath) + ".json");

            if (!File.Exists(outputFile))
            {
                RunMSBuild(solutionPath, outputFile);
            }

            return DependencyGraphSpec.Load(outputFile);
        }

        private void RunMSBuild(string solutionPath, string outputFile)
        {
            var psi = new ProcessStartInfo(_dotnetPath);

            var arguments = new List<string>
            {
                "msbuild",
                $"\"{solutionPath}\"",
                "/t:GenerateRestoreGraphFile",
                "/nologo",
                "/v:q",
                "/p:BuildProjectReferences=false",
                $"/p:RestoreGraphOutputPath=\"{outputFile}\"",
                "/p:KoreBuildRestoreTargetsImported=true",
            };

            psi.Arguments = string.Join(" ", arguments);
            psi.RedirectStandardOutput = true;

            var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true,
            };
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine(args.Data);
                }
            };

            using (process)
            {
                process.Start();
                process.BeginOutputReadLine();

                process.WaitForExit(60 * 5000);
                if (process.ExitCode != 0)
                {
                    throw new Exception($"{psi.FileName} {psi.Arguments} failed. Exit code {process.ExitCode}.");
                }
            }
        }

        public void Dispose()
        {
            if (_deleteSpecDirectoryOnDispose)
            {
                Directory.Delete(_packageSpecDirectory, recursive: true);
            }
        }
    }
}
