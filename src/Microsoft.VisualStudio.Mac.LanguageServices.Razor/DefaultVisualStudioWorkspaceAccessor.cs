// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    [System.Composition.Shared]
    [Export(typeof(VisualStudioWorkspaceAccessor))]
    internal class DefaultVisualStudioWorkspaceAccessor : VisualStudioWorkspaceAccessor
    {
        private readonly TextBufferProjectService _projectService;

        [ImportingConstructor]
        public DefaultVisualStudioWorkspaceAccessor(TextBufferProjectService projectService)
        {
            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _projectService = projectService;
        }

        public override bool TryGetWorkspace(ITextBuffer textBuffer, out Workspace workspace)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            // We do a best effort approach in this method to get the workspace that belongs to the TextBuffer.
            // Below we try and find the project and then the solution that contains the given text buffer. If 
            // we're able to find both the project and solution then we use the solution to look up the corresponding 
            // Workspace using MonoDevelops TypeSystemService.

            var hostProject = (DotNetProject)_projectService.GetHostProject(textBuffer);
            if (hostProject == null)
            {
                // Does not have a host project.
                workspace = null;
                return false;
            }

            var hostSolution = hostProject.ParentSolution;
            if (hostSolution == null)
            {
                // Project does not have a solution
                workspace = null;
                return false;
            }

            workspace = TypeSystemService.GetWorkspace(hostSolution);

            // Workspace cannot be null at this point. If TypeSystemService.GetWorkspace isn't able to find a corresponding
            // workspace it returns an empty workspace. Therefore, in order to see if we have a valid workspace we need to
            // cross-check it against the list of active non-empty workspaces.

            if (!TypeSystemService.AllWorkspaces.Contains(workspace))
            {
                // We were returned the empty workspace which is equivalent to us not finding a valid workspace for our text buffer.
                workspace = null;
                return false;
            }

            return true;
        }
    }
}
