// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    [Guid("079e9499-d150-40af-8876-3047f7942c2a")]
    public class RazorInfoToolWindow : ToolWindowPane
    {
        private ProjectSnapshotManager _projectManager;
        private VisualStudioWorkspace _workspace;

        public RazorInfoToolWindow() : base(null)
        {
            Caption = "Razor Info";
            Content = new RazorInfoToolWindowControl();
        }

        private RazorInfoViewModel DataContext
        {
            get => (RazorInfoViewModel)((RazorInfoToolWindowControl)Content).DataContext;
            set => ((RazorInfoToolWindowControl)Content).DataContext = value;
        }

        protected override void Initialize()
        {
            base.Initialize();

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            _workspace = componentModel.GetService<VisualStudioWorkspace>();

            _projectManager = _workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<ProjectSnapshotManager>();
            _projectManager.Changed += ProjectManager_Changed;

            DataContext = new RazorInfoViewModel(_workspace, _projectManager, OnException);
            
            foreach (var project in _projectManager.Projects)
            {
                DataContext.Projects.Add(new ProjectViewModel(project.FilePath));
            }

            if (DataContext.Projects.Count > 0)
            {
                DataContext.SelectedProject = DataContext.Projects[0];
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _projectManager.Changed -= ProjectManager_Changed;
            }
        }

        private void ProjectManager_Changed(object sender, ProjectChangeEventArgs e)
        {
            DataContext.OnChange(e);
        }

        private void OnException(Exception ex)
        {
            VsShellUtilities.ShowMessageBox(
                this,
                ex.ToString(),
                "Razor Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
#endif
