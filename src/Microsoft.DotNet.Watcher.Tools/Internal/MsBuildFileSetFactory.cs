// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class MsBuildFileSetFactory : IFileSetFactory
    {
        private const string TargetName = "GenerateWatchList";
        private const string ProjectExtensionFileExtension = ".dotnetwatch.targets";
        private const string WatchTargetsFileName = "DotNetWatchCommon.targets";
        private readonly IReporter _reporter;
        private readonly string _projectFile;
        private readonly string _watchTargetsDir;
        private readonly OutputSink _outputSink;
        private readonly ProcessRunner _processRunner;

        public MsBuildFileSetFactory(IReporter reporter, string projectFile)
            : this(reporter, projectFile, new OutputSink())
        {
        }

        // output sink is for testing
        internal MsBuildFileSetFactory(IReporter reporter, string projectFile, OutputSink outputSink)
        {
            Ensure.NotNull(reporter, nameof(reporter));
            Ensure.NotNullOrEmpty(projectFile, nameof(projectFile));
            Ensure.NotNull(outputSink, nameof(outputSink));

            _reporter = reporter;
            _projectFile = projectFile;
            _watchTargetsDir = FindWatchTargetsDir();
            _outputSink = outputSink;
            _processRunner = new ProcessRunner(reporter);
        }

        internal List<string> BuildFlags { get; } = new List<string>
        {
            "/nologo",
            "/v:n",
            "/t:" + TargetName,
            "/p:DotNetWatchBuild=true", // extensibility point for users
            "/p:DesignTimeBuild=true", // don't do expensive things
        };

        public async Task<IFileSet> CreateAsync(CancellationToken cancellationToken)
        {
            EnsureInitialized();

            var watchList = Path.GetTempFileName();
            try
            {
                var projectDir = Path.GetDirectoryName(_projectFile);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var capture = _outputSink.StartCapture();
                    // TODO adding files doesn't currently work. Need to provide a way to detect new files
                    // find files
                    var processSpec = new ProcessSpec
                    {
                        Executable = DotNetMuxer.MuxerPathOrDefault(),
                        WorkingDirectory = projectDir,
                        Arguments = new[]
                        {
                            "msbuild",
                            _projectFile,
                            $"/p:_DotNetWatchTargetsLocation={_watchTargetsDir}", // add our dotnet-watch targets
                            $"/p:_DotNetWatchListFile={watchList}"
                        }.Concat(BuildFlags),
                        OutputCapture = capture
                    };

                    _reporter.Verbose($"Running MSBuild target '{TargetName}' on '{_projectFile}'");

                    var exitCode = await _processRunner.RunAsync(processSpec, cancellationToken);

                    if (exitCode == 0)
                    {
                        var fileset = new FileSet(
                            File.ReadAllLines(watchList)
                                .Select(l => l?.Trim())
                                .Where(l => !string.IsNullOrEmpty(l)));

                        _reporter.Verbose($"Watching {fileset.Count} file(s) for changes");
#if DEBUG

                        foreach (var file in fileset)
                        {
                            _reporter.Verbose($"  -> {file}");
                        }

                        Debug.Assert(fileset.All(Path.IsPathRooted), "All files should be rooted paths");
#endif

                        return fileset;
                    }

                    _reporter.Error($"Error(s) finding watch items project file '{Path.GetFileName(_projectFile)}'");

                    _reporter.Output($"MSBuild output from target '{TargetName}'");

                    foreach (var line in capture.Lines)
                    {
                        _reporter.Output($"  [MSBUILD] : {line}");
                    }

                    _reporter.Warn("Fix the error to continue or press Ctrl+C to exit.");

                    var fileSet = new FileSet(new[] {_projectFile});

                    using (var watcher = new FileSetWatcher(fileSet))
                    {
                        await watcher.GetChangedFileAsync(cancellationToken);

                        _reporter.Output($"File changed: {_projectFile}");
                    }
                }
            }
            finally
            {
                File.Delete(watchList);
            }
        }

        // Ensures file exists in $(MSBuildProjectExtensionsPath)/$(MSBuildProjectFile).dotnetwatch.targets
        private void EnsureInitialized()
        {
            // default value for MSBuildProjectExtensionsPath.
            var projectExtensionsPath = Path.Combine(Path.GetDirectoryName(_projectFile), "obj");

            // see https://github.com/Microsoft/msbuild/blob/bf9b21cc7869b96ea2289ff31f6aaa5e1d525a26/src/XMakeTasks/Microsoft.Common.targets#L127
            var projectExtensionFile = Path.Combine(projectExtensionsPath,
                Path.GetFileName(_projectFile) + ProjectExtensionFileExtension);

            if (!File.Exists(projectExtensionFile))
            {
                // ensure obj folder is available
                Directory.CreateDirectory(Path.GetDirectoryName(projectExtensionFile));

                using (var fileStream = new FileStream(projectExtensionFile, FileMode.Create))
                using (var assemblyStream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream("dotnetwatch.targets"))
                {
                    assemblyStream.CopyTo(fileStream);
                }
            }
        }

        private string FindWatchTargetsDir()
        {
            var assemblyDir = Path.GetDirectoryName(GetType().GetTypeInfo().Assembly.Location);
            var searchPaths = new[]
            {
                AppContext.BaseDirectory,
                assemblyDir,
                Path.Combine(assemblyDir, "../../tools"), // from nuget cache
                Path.Combine(assemblyDir, "tools") // from local build
            };

            var targetPath = searchPaths.Select(p => Path.Combine(p, WatchTargetsFileName)).First(File.Exists);
            return Path.GetDirectoryName(targetPath);
        }
    }
}