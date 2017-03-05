using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using NuGet.ProjectModel;

namespace UniverseTools
{
    public class DependencyGraphSpecProvider : IDisposable
    {
        private readonly string _packageSpecDirectory;
        private readonly bool _deleteSpecDirectoryOnDispose;
        private readonly string _muxerPath;

        public DependencyGraphSpecProvider(string packageSpecDirectory)
            : this(packageSpecDirectory, deleteSpecDirectoryOnDispose: false)
        {
        }

        private DependencyGraphSpecProvider(string packageSpecDirectory, bool deleteSpecDirectoryOnDispose)
        {
            _packageSpecDirectory = packageSpecDirectory;
            _deleteSpecDirectoryOnDispose = deleteSpecDirectoryOnDispose;
            _muxerPath = new Muxer().MuxerPath;
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
            var psi = new ProcessStartInfo(_muxerPath);

            var arguments = new List<string>
            {
                "msbuild",
                $"\"{solutionPath}\"",
                "/t:GenerateRestoreGraphFile",
                "/nologo",
                "/v:q",
                "/p:BuildProjectReferences=false",
                $"/p:RestoreGraphOutputPath=\"{outputFile}\"",
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
