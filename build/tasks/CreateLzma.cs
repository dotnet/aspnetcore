// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.Archive;

namespace RepoTasks
{
    public class CreateLzma : Task, ICancelableTask
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string[] Sources { get; set; }

        public void Cancel() => _cts.Cancel();

        public override bool Execute()
        {
            var progress = new MSBuildProgressReport(Log, _cts.Token);
            using (var archive = new IndexedArchive())
            {
                foreach (var source in Sources)
                {
                    if (Directory.Exists(source))
                    {
                        var trimmedSource = source.TrimEnd(new []{ '\\', '/' });
                        Log.LogMessage(MessageImportance.High, $"Adding directory: {trimmedSource}");
                        archive.AddDirectory(trimmedSource, progress);
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.High, $"Adding file: {source}");
                        archive.AddFile(source, Path.GetFileName(source));
                    }
                }

                archive.Save(OutputPath, progress);
            }

            return !Log.HasLoggedErrors;
        }

        private class MSBuildProgressReport : IProgress<ProgressReport>
        {
            private TaskLoggingHelper _log;
            private readonly CancellationToken _cancellationToken;

            public MSBuildProgressReport(TaskLoggingHelper log, CancellationToken cancellationToken)
            {
                _log = log;
                _cancellationToken = cancellationToken;
            }

            public void Report(ProgressReport value)
            {
                var complete = (double)value.Ticks / value.Total;
                _log.LogMessage(MessageImportance.Low, $"Progress: {value.Phase} - {complete:P}");
                _cancellationToken.ThrowIfCancellationRequested(); // because LZMA apis don't take a cancellation token, throw from the logger (yes, its ugly, but it works.)
            }
        }
    }
}
