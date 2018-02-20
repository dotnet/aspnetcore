// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    // ----------------------------------------------------------------------------------------------------
    // NOTE: This is only here for VisualStudio binary compatibility. This type should not be used; instead
    // use TagHelperResolver.
    // ----------------------------------------------------------------------------------------------------
    [Export(typeof(ITagHelperResolver))]
    internal class LegacyTagHelperResolver : ITagHelperResolver
    {
        private readonly Workspace _workspace;

        [ImportingConstructor]
        public LegacyTagHelperResolver([Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _workspace = workspace;
        }

        public Task<TagHelperResolutionResult> GetTagHelpersAsync(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (project.FilePath == null)
            {
                return Task.FromResult(TagHelperResolutionResult.Empty);
            }

            var projectManager = _workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<ProjectSnapshotManager>();
            var projectSnapshot = projectManager.GetProjectWithFilePath(project.FilePath);
            if (projectSnapshot == null)
            {
                return Task.FromResult(TagHelperResolutionResult.Empty);
            }

            // In 15.6-7 this API is called by WTE to resolve tag helpers, and can be called on build - ie: without any ProjectSnapshot
            // changes. That means that projectSnapshot.WorkspaceProject is out of date with respect to the actual workspace. 
            //
            // To work around this issue, always grab the latest WorkspaceProject with the same ID, which will trigger tag helper
            // discovery on the current state of the workspace.
            //
            // This workaround won't be needed in 15.8 since we do tag helper discovery through the project snapshot manager.
            var latest = _workspace.CurrentSolution.GetProject(projectSnapshot.WorkspaceProject.Id) ?? projectSnapshot.WorkspaceProject;
            if (projectSnapshot.WorkspaceProject != latest)
            {
                projectSnapshot = ((DefaultProjectSnapshot)projectSnapshot).WithWorkspaceProject(latest);
            }
            
            var resolver = _workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<TagHelperResolver>();
            return resolver.GetTagHelpersAsync(projectSnapshot);
        }
    }
}