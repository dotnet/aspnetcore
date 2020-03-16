// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class ProjectToolScenario : IDisposable
    {
        private static readonly string TestProjectSourceRoot = Path.Combine(AppContext.BaseDirectory, "TestProjects");
        private readonly ITestOutputHelper _logger;

        public ProjectToolScenario()
            : this(null)
        {
        }

        public ProjectToolScenario(ITestOutputHelper logger)
        {
            WorkFolder = Path.Combine(AppContext.BaseDirectory, "tmp", Path.GetRandomFileName());
            DotNetWatchPath = Path.Combine(AppContext.BaseDirectory, "tool", "dotnet-watch.dll");

            _logger = logger;
            _logger?.WriteLine($"The temporary test folder is {WorkFolder}");

            CreateTestDirectory();
        }

        public string WorkFolder { get; }

        public string DotNetWatchPath { get; }

        public void AddTestProjectFolder(string projectName)
        {
            var srcFolder = Path.Combine(TestProjectSourceRoot, projectName);
            var destinationFolder = Path.Combine(WorkFolder, Path.GetFileName(projectName));
            _logger?.WriteLine($"Copying project {srcFolder} to {destinationFolder}");

            Directory.CreateDirectory(destinationFolder);

            foreach (var directory in Directory.GetDirectories(srcFolder, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(srcFolder, destinationFolder));
            }

            foreach (var file in Directory.GetFiles(srcFolder, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(srcFolder, destinationFolder), true);
            }
        }

        public Task RestoreAsync(string project)
        {
            _logger?.WriteLine($"Restoring msbuild project in {project}");
            return ExecuteCommandAsync(project, TimeSpan.FromSeconds(120), "restore", "--ignore-failed-sources");
        }

        public Task BuildAsync(string project)
        {
            _logger?.WriteLine($"Building {project}");
            return ExecuteCommandAsync(project, TimeSpan.FromSeconds(60), "build");
        }

        private async Task ExecuteCommandAsync(string project, TimeSpan timeout, params string[] arguments)
        {
            var tcs = new TaskCompletionSource<object>();
            project = Path.Combine(WorkFolder, project);
            _logger?.WriteLine($"Project directory: '{project}'");

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = DotNetMuxer.MuxerPathOrDefault(),
                    Arguments = ArgumentEscaper.EscapeAndConcatenate(arguments),
                    WorkingDirectory = project,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Environment =
                    {
                        ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true"
                    }
                },
            };

            void OnData(object sender, DataReceivedEventArgs args)
              => _logger?.WriteLine(args.Data ?? string.Empty);

            void OnExit(object sender, EventArgs args)
            {
                _logger?.WriteLine($"Process exited {process.Id}");
                tcs.TrySetResult(null);
            }

            process.ErrorDataReceived += OnData;
            process.OutputDataReceived += OnData;
            process.Exited += OnExit;

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            _logger?.WriteLine($"Started process {process.Id}: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            var done = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            process.CancelErrorRead();
            process.CancelOutputRead();

            process.ErrorDataReceived -= OnData;
            process.OutputDataReceived -= OnData;
            process.Exited -= OnExit;

            if (!ReferenceEquals(done, tcs.Task))
            {
                if (!process.HasExited)
                {
                    _logger?.WriteLine($"Killing process {process.Id}");
                    process.KillTree();
                }

                throw new TimeoutException($"Process timed out after {timeout.TotalSeconds} seconds");
            }

            _logger?.WriteLine($"Process exited {process.Id} with code {process.ExitCode}");
            if (process.ExitCode != 0)
            {

                throw new InvalidOperationException($"Exit code {process.ExitCode}");
            }
        }

        private void CreateTestDirectory()
        {
            Directory.CreateDirectory(WorkFolder);

            File.WriteAllText(Path.Combine(WorkFolder, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(WorkFolder, "Directory.Build.targets"), "<Project />");
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(WorkFolder, recursive: true);
            }
            catch
            {
                Console.WriteLine($"Failed to delete {WorkFolder}. Retrying...");
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Directory.Delete(WorkFolder, recursive: true);
            }
        }
    }
}
