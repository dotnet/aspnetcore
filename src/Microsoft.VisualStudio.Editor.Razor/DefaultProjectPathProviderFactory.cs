// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [ExportWorkspaceService(typeof(ProjectPathProvider), ServiceLayer.Default)]
    internal class DefaultProjectPathProviderFactory : IWorkspaceServiceFactory
    {
        private readonly TextBufferProjectService _projectService;

        [ImportingConstructor]
        public DefaultProjectPathProviderFactory(TextBufferProjectService projectService)
        {
            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _projectService = projectService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            return new DefaultProjectPathProvider(_projectService);
        }
    }
}
