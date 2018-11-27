// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace Microsoft.VisualStudio.Mac.RazorAddin
{
    internal class RazorProjectExtension : ProjectExtension
    {
        private readonly object _lock = new object();
        private readonly ForegroundDispatcher _foregroundDispatcher;

        public RazorProjectExtension()
        {
            _foregroundDispatcher = CompositionManager.GetExportedValue<ForegroundDispatcher>();
        }

        protected override void OnBoundToSolution()
        {
            if (!(Project is DotNetProject dotNetProject))
            {
                return;
            }

            DotNetProjectHost projectHost;
            lock (_lock)
            {
                if (Project.ExtendedProperties.Contains(typeof(DotNetProjectHost)))
                {
                    // Already have a project host.
                    return;
                }

                var projectHostFactory = CompositionManager.GetExportedValue<DotNetProjectHostFactory>();
                projectHost = projectHostFactory.Create(dotNetProject);
                Project.ExtendedProperties[typeof(DotNetProjectHost)] = projectHost;
            }

            // Once a workspace is created for the solution we'll setup our project host for the current project. The Razor world
            // shares a lifetime with the workspace (as Roslyn services) so we need to ensure it exists prior to wiring the host
            // world to the Roslyn world.
            TypeSystemService.GetWorkspaceAsync(Project.ParentSolution).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    // We only want to act if we could properly retrieve the workspace.
                    return;
                }

                projectHost.Subscribe();
            },
            _foregroundDispatcher.ForegroundScheduler);
        }
    }
}
