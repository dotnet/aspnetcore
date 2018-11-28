// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.BlazorExtension
{
    // We mainly have a package so we can have an "About" dialog entry.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [AboutDialogInfo(PackageGuidString, "ASP.NET Core Blazor Language Services", "#110", "112")]
    [Guid(BlazorPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class BlazorPackage : Package
    {
        public const string PackageGuidString = "d9fe04bc-57a7-4107-915e-3a5c2f9e19fb";

        protected override void Initialize()
        {
            base.Initialize();
            RegisterAutoRebuildService();
        }

        private void RegisterAutoRebuildService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Create build watcher. No need to unadvise, as this only happens once anyway.
            var solution = (IVsSolution)GetGlobalService(typeof(IVsSolution));
            var buildManager = (IVsSolutionBuildManager)GetService(typeof(SVsSolutionBuildManager));
            var buildWatcher = new BuildEventsWatcher(solution, buildManager);
            var hr = buildManager.AdviseUpdateSolutionEvents(buildWatcher, out var cookie);
            Marshal.ThrowExceptionForHR(hr);

            new AutoRebuildService(buildWatcher).Listen();
        }
    }
}
