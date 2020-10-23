// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class MsBuildFileSetFactory : IFileSetFactory
    {
        private const string TargetName = "GenerateWatchList";
        private const string WatchTargetsFileName = "DotNetWatch.targets";
        private readonly IReporter _reporter;
        private readonly DotNetWatchOptions _dotNetWatchOptions;
        private readonly string _projectFile;
        private readonly OutputSink _outputSink;
        private readonly ProcessRunner _processRunner;
        private readonly bool _waitOnError;
        private readonly IReadOnlyList<string> _buildFlags;

        public MsBuildFileSetFactory(
            IReporter reporter,
            DotNetWatchOptions dotNetWatchOptions,
            string projectFile,
            bool waitOnError,
            bool trace)
            : this(reporter, dotNetWatchOptions, projectFile, new OutputSink(), trace)
        {
            _waitOnError = waitOnError;
        }

        // output sink is for testing
        internal MsBuildFileSetFactory(IReporter reporter,
            DotNetWatchOptions dotNetWatchOptions,
            string projectFile,
            OutputSink outputSink,
            bool trace)
        {
            Ensure.NotNull(reporter, nameof(reporter));
            Ensure.NotNullOrEmpty(projectFile, nameof(projectFile));
            Ensure.NotNull(outputSink, nameof(outputSink));

            _reporter = reporter;
            _dotNetWatchOptions = dotNetWatchOptions;
            _projectFile = projectFile;
            _outputSink = outputSink;
            _processRunner = new ProcessRunner(reporter);
            _buildFlags = InitializeArgs(FindTargetsFile(), trace);
        }

        public async Task<FileSet> CreateAsync(CancellationToken cancellationToken)
        {
            var watchList = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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
                            "/nologo",
                            _projectFile,
                            $"/p:_DotNetWatchListFile={watchList}",
                            _dotNetWatchOptions.SuppressHandlingStaticContentFiles ? "/p:DotNetWatchContentFiles=false" : "",
                        }.Concat(_buildFlags),
                        OutputCapture = capture
                    };

                    _reporter.Verbose($"Running MSBuild target '{TargetName}' on '{_projectFile}'");

                    var exitCode = await _processRunner.RunAsync(processSpec, cancellationToken);

                    if (exitCode == 0 && File.Exists(watchList))
                    {
                        var lines = File.ReadAllLines(watchList);

                        var staticFiles = new List<string>();
                        var commandLine = new CommandLineApplication();
                        var contentFiles = commandLine.Option("-c", "Content file", CommandOptionType.MultipleValue);
                        var contentFilePaths = commandLine.Option("-s", "Static asset path", CommandOptionType.MultipleValue);
                        var files = commandLine.Option("-f", "Watched files", CommandOptionType.MultipleValue);
                        var isNetCoreApp31 = commandLine.Option("-isnetcoreapp31", "Is .NET Core 3.1 or newer?", CommandOptionType.NoValue);
                        commandLine.Invoke = () => 0;

                        commandLine.Execute(lines);
                        var isNetCoreApp31OrNewer = isNetCoreApp31.Value();
                        var fileItems = new List<FileItem>();
                        foreach (var file in files.Values)
                        {
                            fileItems.Add(new FileItem(file));
                        }

                        // Ignore static files if we do not support it.
                        for (var i = 0; i < contentFiles.Values.Count; i++)
                        {
                            var contentFile = contentFiles.Values[i];
                            var staticWebAssetPath = contentFilePaths.Values[i].TrimStart('/');

                            fileItems.Add(new FileItem(contentFile, FileKind.StaticFile, staticWebAssetPath));
                        }

                        var fileset = new FileSet(isNetCoreApp31.HasValue(), fileItems);

                        _reporter.Verbose($"Watching {fileset.Count} file(s) for changes");
#if DEBUG

                        foreach (var file in fileset)
                        {
                            _reporter.Verbose($"  -> {file.FilePath} {file.FileKind} {file.StaticWebAssetPath}.");
                        }

                        Debug.Assert(fileset.All(f => Path.IsPathRooted(f.FilePath)), "All files should be rooted paths");
#endif

                        return fileset;
                    }

                    _reporter.Error($"Error(s) finding watch items project file '{Path.GetFileName(_projectFile)}'");

                    _reporter.Output($"MSBuild output from target '{TargetName}':");
                    _reporter.Output(string.Empty);

                    foreach (var line in capture.Lines)
                    {
                        _reporter.Output($"   {line}");
                    }

                    _reporter.Output(string.Empty);

                    if (!_waitOnError)
                    {
                        return null;
                    }
                    else
                    {
                        _reporter.Warn("Fix the error to continue or press Ctrl+C to exit.");

                        var fileSet = new FileSet(false, new[] { new FileItem(_projectFile) });

                        using (var watcher = new FileSetWatcher(fileSet, _reporter))
                        {
                            await watcher.GetChangedFileAsync(cancellationToken);

                            _reporter.Output($"File changed: {_projectFile}");
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(watchList))
                {
                    File.Delete(watchList);
                }
            }
        }

        private IReadOnlyList<string> InitializeArgs(string watchTargetsFile, bool trace)
        {
            var args = new List<string>
            {
                "/nologo",
                "/v:n",
                "/t:" + TargetName,
                "/p:DotNetWatchBuild=true", // extensibility point for users
                "/p:DesignTimeBuild=true", // don't do expensive things
                "/p:CustomAfterMicrosoftCommonTargets=" + watchTargetsFile,
                "/p:CustomAfterMicrosoftCommonCrossTargetingTargets=" + watchTargetsFile,
            };

            if (trace)
            {
                // enables capturing markers to know which projects have been visited
                args.Add("/p:_DotNetWatchTraceOutput=true");
            }

            return args;
        }

        private string FindTargetsFile()
        {
            var assemblyDir = Path.GetDirectoryName(typeof(MsBuildFileSetFactory).Assembly.Location);
            var searchPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "assets"),
                Path.Combine(assemblyDir, "assets"),
                AppContext.BaseDirectory,
                assemblyDir,
            };

            var targetPath = searchPaths.Select(p => Path.Combine(p, WatchTargetsFileName)).FirstOrDefault(File.Exists);
            if (targetPath == null)
            {
                _reporter.Error("Fatal error: could not find DotNetWatch.targets");
                return null;
            }
            return targetPath;
        }
    }
}
