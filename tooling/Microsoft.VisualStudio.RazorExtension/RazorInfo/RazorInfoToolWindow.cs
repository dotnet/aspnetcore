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
        private IRazorEngineDocumentGenerator _documentGenerator;
        private IRazorEngineDirectiveResolver _directiveResolver;
        private ProjectSnapshotManager _projectManager;
        private TagHelperResolver _tagHelperResolver;
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

            _documentGenerator = componentModel.GetService<IRazorEngineDocumentGenerator>();
            _directiveResolver = componentModel.GetService<IRazorEngineDirectiveResolver>();
            _tagHelperResolver = _workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<TagHelperResolver>();

            _projectManager = _workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<ProjectSnapshotManager>();
            _projectManager.Changed += ProjectManager_Changed;

            DataContext = new RazorInfoViewModel(this, _workspace, _projectManager, _directiveResolver, _tagHelperResolver, _documentGenerator, OnException);
            foreach (var project in _projectManager.Projects)
            {
                DataContext.Projects.Add(new ProjectViewModel(project.FilePath)
                {
                    Snapshot = new ProjectSnapshotViewModel(project),
                });
            }

            if (DataContext.Projects.Count > 0)
            {
                DataContext.CurrentProject = DataContext.Projects[0];
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
            switch (e.Kind)
            {
                case ProjectChangeKind.Added:
                    {
                        var added = new ProjectViewModel(e.Project.FilePath)
                        {
                            Snapshot = new ProjectSnapshotViewModel(e.Project),
                        };

                        DataContext.Projects.Add(added);

                        if (DataContext.Projects.Count == 1)
                        {
                            DataContext.CurrentProject = added;
                        }
                        break;
                    }

                case ProjectChangeKind.Removed:
                    {
                        ProjectViewModel removed = null;
                        for (var i = DataContext.Projects.Count - 1; i >= 0; i--)
                        {
                            var project = DataContext.Projects[i];
                            if (project.FilePath == e.Project.FilePath)
                            {
                                removed = project;
                                DataContext.Projects.RemoveAt(i);
                                break;
                            }
                        }

                        if (DataContext.CurrentProject == removed)
                        {
                            DataContext.CurrentProject = null;
                        }

                        break;
                    }

                case ProjectChangeKind.Changed:
                    {
                        ProjectViewModel changed = null;
                        for (var i = DataContext.Projects.Count - 1; i >= 0; i--)
                        {
                            var project = DataContext.Projects[i];
                            if (project.FilePath == e.Project.FilePath)
                            {
                                changed = project;
                                changed.Snapshot = new ProjectSnapshotViewModel(e.Project);
                                break;
                            }
                        }
                        
                        break;
                    }
            }
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
