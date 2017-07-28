// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
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
        private IRazorEngineAssemblyResolver _assemblyResolver;
        private IRazorEngineDocumentGenerator _documentGenerator;
        private IRazorEngineDirectiveResolver _directiveResolver;
        private TagHelperResolver _tagHelperResolver;
        private VisualStudioWorkspace _workspace;

        public RazorInfoToolWindow() : base(null)
        {
            this.Caption = "Razor Info";
            this.Content = new RazorInfoToolWindowControl();
        }

        protected override void Initialize()
        {
            base.Initialize();

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));

            _assemblyResolver = componentModel.GetService<IRazorEngineAssemblyResolver>();
            _documentGenerator = componentModel.GetService<IRazorEngineDocumentGenerator>();
            _directiveResolver = componentModel.GetService<IRazorEngineDirectiveResolver>();
            _tagHelperResolver = componentModel.GetService<TagHelperResolver>();

            _workspace = componentModel.GetService<VisualStudioWorkspace>();
            _workspace.WorkspaceChanged += Workspace_WorkspaceChanged;

            Reset(_workspace.CurrentSolution);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
            }
        }

        private void Reset(Solution solution)
        {
            if (solution == null)
            {
                ((RazorInfoToolWindowControl)this.Content).DataContext = null;
                return;
            }

            var viewModel = new RazorInfoViewModel(this, _workspace, _assemblyResolver, _directiveResolver, _tagHelperResolver, _documentGenerator, OnException);
            foreach (var project in solution.Projects)
            {
                if (project.Language == LanguageNames.CSharp)
                {
                    viewModel.Projects.Add(new ProjectViewModel(project));
                }
            }

            ((RazorInfoToolWindowControl)this.Content).DataContext = viewModel;
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

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:
                    Reset(e.NewSolution);
                    break;
            }
        }
    }
}
#endif
