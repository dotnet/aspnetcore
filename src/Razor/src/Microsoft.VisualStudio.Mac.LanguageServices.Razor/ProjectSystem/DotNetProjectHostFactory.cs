// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using MonoDevelop.Projects;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    [System.Composition.Shared]
    [Export(typeof(DotNetProjectHostFactory))]
    internal class DotNetProjectHostFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly VisualStudioMacWorkspaceAccessor _workspaceAccessor;
        private readonly TextBufferProjectService _projectService;

        [ImportingConstructor]
        public DotNetProjectHostFactory(
            ForegroundDispatcher foregroundDispatcher,
            VisualStudioMacWorkspaceAccessor workspaceAccessor,
            TextBufferProjectService projectService)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (workspaceAccessor == null)
            {
                throw new ArgumentNullException(nameof(workspaceAccessor));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _workspaceAccessor = workspaceAccessor;
            _projectService = projectService;
        }

        public DotNetProjectHost Create(DotNetProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var projectHost = new DefaultDotNetProjectHost(project, _foregroundDispatcher, _workspaceAccessor, _projectService);
            return projectHost;
        }
    }
}
