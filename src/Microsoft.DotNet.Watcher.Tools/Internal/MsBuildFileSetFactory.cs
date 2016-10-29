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
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Watcher.Tools;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class MsBuildFileSetFactory : IFileSetFactory
    {
        private const string ProjectExtensionFileExtension = ".dotnetwatch.targets";
        private const string WatchTargetsFileName = "DotNetWatchCommon.targets";
        private readonly ILogger _logger;
        private readonly string _projectFile;
        private readonly string _watchTargetsDir;
        private readonly OutputSink _outputSink;

        public MsBuildFileSetFactory(ILogger logger, string projectFile)
            : this(logger, projectFile, new OutputSink())
        {
        }

        // output sink is for testing
        internal MsBuildFileSetFactory(ILogger logger, string projectFile, OutputSink outputSink)
        {
            Ensure.NotNull(logger, nameof(logger));
            Ensure.NotNullOrEmpty(projectFile, nameof(projectFile));
            Ensure.NotNull(outputSink, nameof(outputSink));

            _logger = logger;
            _projectFile = projectFile;
            _watchTargetsDir = FindWatchTargetsDir();
            _outputSink = outputSink;
        }

        internal List<string> BuildFlags { get; } = new List<string>
        {
            "/nologo",
            "/v:n",
            "/t:GenerateWatchList",
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

#if DEBUG
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
#endif
                    var capture = _outputSink.StartCapture();
                    // TODO adding files doesn't currently work. Need to provide a way to detect new files
                    // find files
                    var exitCode = Command.CreateDotNet("msbuild",
                        new[]
                        {
                            _projectFile,
                            $"/p:_DotNetWatchTargetsLocation={_watchTargetsDir}", // add our dotnet-watch targets
                            $"/p:_DotNetWatchListFile={watchList}",
                        }.Concat(BuildFlags))
                        .CaptureStdErr()
                        .CaptureStdOut()
                        .OnErrorLine(l => capture.WriteErrorLine(l))
                        .OnOutputLine(l => capture.WriteOutputLine(l))
                        .WorkingDirectory(projectDir)
                        .Execute()
                        .ExitCode;

                    if (exitCode == 0)
                    {
                        var files = File.ReadAllLines(watchList)
                                .Select(l => l?.Trim())
                                .Where(l => !string.IsNullOrEmpty(l));

                        var fileset = new FileSet(files);

#if DEBUG
                        _logger.LogDebug(string.Join(Environment.NewLine, fileset));
                        Debug.Assert(files.All(Path.IsPathRooted), "All files should be rooted paths");
                        stopwatch.Stop();
                        _logger.LogDebug("Gathered project information in {time}ms", stopwatch.ElapsedMilliseconds);
#endif

                        return fileset;
                    }

                    _logger.LogError($"Error(s) finding watch items project file '{Path.GetFileName(_projectFile)}': ");
                    _logger.LogError(capture.GetAllLines("[MSBUILD] : "));
                    _logger.LogInformation("Fix the error to continue.");

                    var fileSet = new FileSet(new[] { _projectFile });

                    using (var watcher = new FileSetWatcher(fileSet))
                    {
                        await watcher.GetChangedFileAsync(cancellationToken);

                        _logger.LogInformation($"File changed: {_projectFile}");
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
            var projectExtensionFile = Path.Combine(projectExtensionsPath, Path.GetFileName(_projectFile) + ProjectExtensionFileExtension);

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
