// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class MSBuildEvaluationFilter : IWatchFilter
    {
        // File types that require an MSBuild re-evaluation
        private static readonly string[] _msBuildFileExtensions = new[]
        {
            ".csproj", ".props", ".targets", ".fsproj", ".vbproj", ".vcxproj",
        };
        private static readonly int[] _msBuildFileExtensionHashes = _msBuildFileExtensions
            .Select(e => e.GetHashCode(StringComparison.OrdinalIgnoreCase))
            .ToArray();

        private readonly IFileSetFactory _factory;

        private List<(string fileName, DateTime lastWriteTimeUtc)> _msbuildFileTimestamps;

        public MSBuildEvaluationFilter(IFileSetFactory factory)
        {
            _factory = factory;
        }

        public async ValueTask ProcessAsync(DotNetWatchContext context, CancellationToken cancellationToken)
        {
            if (context.SuppressMSBuildIncrementalism)
            {
                context.RequiresMSBuildRevaluation = true;
                context.FileSet = await _factory.CreateAsync(cancellationToken);
                return;
            }

            if (context.Iteration == 0 || RequiresMSBuildRevaluation(context))
            {
                context.RequiresMSBuildRevaluation = true;
            }

            if (context.RequiresMSBuildRevaluation)
            {
                context.Reporter.Verbose("Evaluating dotnet-watch file set.");

                context.FileSet = await _factory.CreateAsync(cancellationToken);
                _msbuildFileTimestamps = GetMSBuildFileTimeStamps(context);
            }
        }

        private bool RequiresMSBuildRevaluation(DotNetWatchContext context)
        {
            var changedFile = context.ChangedFile;
            if (changedFile != null && IsMsBuildFileExtension(changedFile.Value.FilePath))
            {
                return true;
            }

            // The filewatcher may miss changes to files. For msbuild files, we can verify that they haven't been modified
            // since the previous iteration.
            // We do not have a way to identify renames or new additions that the file watcher did not pick up,
            // without performing an evaluation. We will start off by keeping it simple and comparing the timestamps
            // of known MSBuild files from previous run. This should cover the vast majority of cases.

            foreach (var (file, lastWriteTimeUtc) in _msbuildFileTimestamps)
            {
                if (GetLastWriteTimeUtcSafely(file) != lastWriteTimeUtc)
                {
                    context.Reporter.Verbose($"Re-evaluation needed due to changes in {file}.");

                    return true;
                }
            }

            return false;
        }

        private List<(string fileName, DateTime lastModifiedUtc)> GetMSBuildFileTimeStamps(DotNetWatchContext context)
        {
            var msbuildFiles = new List<(string fileName, DateTime lastModifiedUtc)>();
            foreach (var file in context.FileSet)
            {
                if (!string.IsNullOrEmpty(file.FilePath) && IsMsBuildFileExtension(file.FilePath))
                {
                    msbuildFiles.Add((file.FilePath, GetLastWriteTimeUtcSafely(file.FilePath)));
                }
            }

            return msbuildFiles;
        }

        protected virtual DateTime GetLastWriteTimeUtcSafely(string file)
        {
            try
            {
                return File.GetLastWriteTimeUtc(file);
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        static bool IsMsBuildFileExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName.AsSpan());
            var hashCode = string.GetHashCode(extension, StringComparison.OrdinalIgnoreCase);
            for (var i = 0; i < _msBuildFileExtensionHashes.Length; i++)
            {
                if (_msBuildFileExtensionHashes[i] == hashCode && extension.Equals(_msBuildFileExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
