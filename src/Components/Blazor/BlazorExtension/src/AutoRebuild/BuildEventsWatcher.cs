// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.BlazorExtension
{
    /// <summary>
    /// Watches for Blazor project build events, starts new builds, and tracks builds in progress.
    /// </summary>
    internal class BuildEventsWatcher : IVsUpdateSolutionEvents2
    {
        private const string BlazorProjectCapability = "Blazor";
        private readonly IVsSolution _vsSolution;
        private readonly IVsSolutionBuildManager _vsBuildManager;
        private readonly object mostRecentBuildInfosLock = new object();
        private readonly Dictionary<string, BuildInfo> mostRecentBuildInfos
            = new Dictionary<string, BuildInfo>(StringComparer.OrdinalIgnoreCase);

        public BuildEventsWatcher(IVsSolution vsSolution, IVsSolutionBuildManager vsBuildManager)
        {
            _vsSolution = vsSolution ?? throw new ArgumentNullException(nameof(vsSolution));
            _vsBuildManager = vsBuildManager ?? throw new ArgumentNullException(nameof(vsBuildManager));
        }

        public Task<bool> PerformBuildAsync(string projectPath, DateTime allowExistingBuildsSince)
        {
            BuildInfo newBuildInfo;

            lock (mostRecentBuildInfosLock)
            {
                if (mostRecentBuildInfos.TryGetValue(projectPath, out var existingInfo))
                {
                    // If there's a build in progress, we'll join that even if it was started
                    // before allowExistingBuildsSince, because it's too messy to cancel
                    // in-progress builds. On rare occasions if the user is editing files while
                    // a build is in progress they *might* see a not-latest build when they
                    // reload, but then they just have to reload again.
                    var acceptBuild = !existingInfo.TaskCompletionSource.Task.IsCompleted
                        || existingInfo.StartTime > allowExistingBuildsSince;
                    if (acceptBuild)
                    {
                        return existingInfo.TaskCompletionSource.Task;
                    }
                }

                // We're going to start a new build now. Track the BuildInfo for it even
                // before it starts so other incoming build requests can join it.
                mostRecentBuildInfos[projectPath] = newBuildInfo = new BuildInfo();
            }

            return PerformNewBuildAsync(projectPath, newBuildInfo);
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
           => VSConstants.S_OK;

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
           => VSConstants.S_OK;

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
           => VSConstants.S_OK;

        public int UpdateSolution_Cancel()
           => VSConstants.S_OK;

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
           => VSConstants.S_OK;

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            if (IsBlazorProject(pHierProj))
            {
                // This method runs both for manually-invoked builds and for builds triggered automatically
                // by PerformNewBuildAsync(). In the case where it's a manually-invoked build, make sure
                // there's an in-progress BuildInfo so that if there are further builds requests while the
                // build is still in progress we can join them onto this existing build.
                var ctx = (IVsBrowseObjectContext)pCfgProj;
                var projectPath = ctx.UnconfiguredProject.FullPath;
                lock (mostRecentBuildInfosLock)
                {
                    var hasBuildInProgress =
                        mostRecentBuildInfos.TryGetValue(projectPath, out var existingInfo)
                        && !existingInfo.TaskCompletionSource.Task.IsCompleted;
                    if (!hasBuildInProgress)
                    {
                        mostRecentBuildInfos[projectPath] = new BuildInfo();
                    }
                }
            }

            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            if (IsBlazorProject(pHierProj))
            {
                var buildResult = fSuccess == 1;
                var ctx = (IVsBrowseObjectContext)pCfgProj;
                var projectPath = ctx.UnconfiguredProject.FullPath;

                // Mark pending build info as completed
                BuildInfo foundInfo = null;
                lock (mostRecentBuildInfosLock)
                {
                    mostRecentBuildInfos.TryGetValue(projectPath, out foundInfo);
                }
                if (foundInfo != null)
                {
                    foundInfo.TaskCompletionSource.TrySetResult(buildResult);
                }
            }

            return VSConstants.S_OK;
        }

        private async Task<bool> PerformNewBuildAsync(string projectPath, BuildInfo buildInfo)
        {
            // Switch to the UI thread and request the build
            var didStartBuild = await ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var hr = _vsSolution.GetProjectOfUniqueName(projectPath, out var hierarchy);
                if (hr != VSConstants.S_OK)
                {
                    return false;
                }

                hr = _vsBuildManager.StartSimpleUpdateProjectConfiguration(
                    hierarchy,
                    /* not used */ null,
                    /* not used */ null,
                    (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD,
                    /* other flags */ 0,
                    /* suppress dialogs */ 1);
                if (hr != VSConstants.S_OK)
                {
                    return false;
                }

                return true;
            });

            if (!didStartBuild)
            {
                // Since the build didn't start, make sure nobody's waiting for it
                buildInfo.TaskCompletionSource.TrySetResult(false);
            }

            return await buildInfo.TaskCompletionSource.Task;
        }

        private static bool IsBlazorProject(IVsHierarchy pHierProj)
            => pHierProj.IsCapabilityMatch(BlazorProjectCapability);

        class BuildInfo
        {
            public DateTime StartTime { get; }
            public TaskCompletionSource<bool> TaskCompletionSource { get; }

            public BuildInfo()
            {
                StartTime = DateTime.Now;
                TaskCompletionSource = new TaskCompletionSource<bool>();
            }
        }
    }
}
