// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.BlazorExtension
{
    // We mainly have a package so we can have an "About" dialog entry.
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [AboutDialogInfo(PackageGuidString, "ASP.NET Core Blazor Language Services", "#110", "112")]
    [Guid(BlazorPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class BlazorPackage : AsyncPackage
    {
        public const string PackageGuidString = "d9fe04bc-57a7-4107-915e-3a5c2f9e19fb";

        protected async override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            // Create build watcher. No need to unadvise, as this only happens once anyway.
            var solution = (IVsSolution)await GetServiceAsync(typeof(IVsSolution));
            var buildManager = (IVsSolutionBuildManager)await GetServiceAsync(typeof(SVsSolutionBuildManager));

            // According to the docs, this can happen if VS shuts down while our package is loading.
            if (solution == null || buildManager == null)
            {
                var buildWatcher = new BuildEventsWatcher(solution, buildManager);
                var hr = buildManager.AdviseUpdateSolutionEvents(buildWatcher, out var cookie);
                Marshal.ThrowExceptionForHR(hr);

                new AutoRebuildService(buildWatcher).Listen();
            }
        }
    }
}
