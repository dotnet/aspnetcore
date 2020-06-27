// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class MSBuildEvaluationFilter : IWatchFilter
    {
        // File types that require an MSBuild re-evaluation
        private static readonly HashSet<string> _msBuildFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".props", ".targets", ".csproj", ".fsproj", ".vbproj",
        };
        private readonly IFileSetFactory _factory;

        public MSBuildEvaluationFilter(IFileSetFactory factory)
        {
            _factory = factory;
        }

        public async ValueTask ProcessAsync(DotNetWatchContext context, CancellationToken cancellationToken)
        {
            if (context.Iteration == 0 || RequiresMSBuildRevaluation(context.ChangedFile))
            {
                context.RequiresMSBuildRevaluation = true;
            }

            if (context.RequiresMSBuildRevaluation)
            {
                context.Reporter.Verbose("Evaluating dotnet-watch file set.");
                context.FileSet = await _factory.CreateAsync(cancellationToken);
            }
        }

        private static bool RequiresMSBuildRevaluation(string changedFile)
        {
            if (string.IsNullOrEmpty(changedFile))
            {
                return false;
            }

            var extension = Path.GetExtension(changedFile);
            return !string.IsNullOrEmpty(extension) && _msBuildFileExtensions.Contains(extension);
        }
    }
}
