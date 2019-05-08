// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.AspNetCore.Analyzers.FeatureDetection
{
    // Be very careful making changes to this file. No project in this repo builds it.
    //
    // If you need to verify a change, make a local project (net472) and copy in everything included by this project.
    //
    // You'll also need some nuget packages like:
    // - Microsoft.VisualStudio.LanguageServices
    // - Microsoft.VisualStudio.Shell.15.0
    // - Microsoft.VisualStudio.Threading
    [Export(typeof(ProjectCompilationFeatureDetector))]
    internal class ProjectCompilationFeatureDetector
    {
        private readonly Lazy<VisualStudioWorkspace> _workspace;

        [ImportingConstructor]
        public ProjectCompilationFeatureDetector(Lazy<VisualStudioWorkspace> workspace)
        {
            _workspace = workspace;
        }

        public async Task<IImmutableSet<string>> DetectFeaturesAsync(string projectFullPath, CancellationToken cancellationToken = default)
        {
            if (projectFullPath == null)
            {
                throw new ArgumentNullException(nameof(projectFullPath));
            }

            // If the workspace is uninitialized, we need to do the first access on the UI thread.
            //
            // This is very unlikely to occur, but doing it here for completeness.
            if (!_workspace.IsValueCreated)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                GC.KeepAlive(_workspace.Value);
                await TaskScheduler.Default;
            }

            var workspace = _workspace.Value;
            var solution = workspace.CurrentSolution;

            var project = GetProject(solution, projectFullPath);
            if (project == null)
            {
                // Cannot find matching project.
                return ImmutableHashSet<string>.Empty;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            return await CompilationFeatureDetector.DetectFeaturesAsync(compilation, cancellationToken);
        }

        private static Project GetProject(Solution solution, string projectFilePath)
        {
            foreach (var project in solution.Projects)
            {
                if (string.Equals(projectFilePath, project.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    return project;
                }
            }

            return null;
        }
    }
}
