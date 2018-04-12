// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    internal class RazorInfoViewModel : NotifyPropertyChanged
    {
        private readonly IServiceProvider _services;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly Workspace _workspace;
        private readonly Action<Exception> _errorHandler;

        private DocumentSnapshotViewModel _currentDocument;
        private ProjectViewModel _currentProject;
        private ProjectInfoViewModel _currentProjectInfo;
        private ICommand _updateCommand;

        public RazorInfoViewModel(
            IServiceProvider services,
            Workspace workspace,
            ProjectSnapshotManager projectManager,
            Action<Exception> errorHandler)
        {
            _services = services;
            _workspace = workspace;
            _projectManager = projectManager;
            _errorHandler = errorHandler;
            
            UpdateCommand = new RelayCommand<object>(ExecuteUpdate, CanExecuteUpdate);
        }

        public DocumentSnapshotViewModel CurrentDocument
        {
            get { return _currentDocument; }
            set
            {
                _currentDocument = value;
                OnPropertyChanged();
            }
        }

        public ProjectViewModel CurrentProject
        {
            get { return _currentProject; }
            set
            {
                _currentProject = value;
                OnPropertyChanged();

                LoadProjectInfo(_currentProject.Snapshot.Project);
            }
        }

        public ProjectInfoViewModel CurrentProjectInfo
        {
            get { return _currentProjectInfo; }
            set
            {
                _currentProjectInfo = value;
                OnPropertyChanged();
            }
        }

        public ICommand UpdateCommand
        {
            get { return _updateCommand; }
            set
            {
                _updateCommand = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ProjectViewModel> Projects { get; } = new ObservableCollection<ProjectViewModel>();

        private bool CanExecuteUpdate(object state)
        {
            return CurrentProject?.Snapshot?.Project.WorkspaceProject != null;
        }

        private void ExecuteUpdate(object state)
        {
            var projectId = CurrentProject?.Snapshot?.Project?.WorkspaceProject?.Id;
            if (projectId != null)
            {
                var solution = _workspace.CurrentSolution;
                var project = solution.GetProject(projectId);
                if (project != null)
                {
                    ((ProjectSnapshotManagerBase)_projectManager).WorkspaceProjectChanged(project);
                }
            }
        }

        private async void LoadProjectInfo(ProjectSnapshot snapshot)
        {
            CurrentProjectInfo = new ProjectInfoViewModel();

            if (snapshot == null)
            {
                return;
            }

            var projectEngine = snapshot.GetProjectEngine();
            var feature = projectEngine.EngineFeatures.OfType<IRazorDirectiveFeature>().FirstOrDefault();
            var directives = feature?.Directives ?? Array.Empty<DirectiveDescriptor>();
            CurrentProjectInfo.Directives = new ObservableCollection<DirectiveDescriptorViewModel>(directives.Select(d => new DirectiveDescriptorViewModel(d)));

            var documents = snapshot.DocumentFilePaths.Select(d => snapshot.GetDocument(d));
            CurrentProjectInfo.Documents = new ObservableCollection<DocumentSnapshotViewModel>(documents.Select(d => new DocumentSnapshotViewModel(d)));

            if (snapshot.TryGetTagHelpers(out var tagHelpers))
            {
                CurrentProjectInfo.TagHelpers = new ObservableCollection<TagHelperViewModel>(tagHelpers.Select(t => new TagHelperViewModel(t)));
            }
            else
            {
                CurrentProjectInfo.TagHelpers = new ObservableCollection<TagHelperViewModel>();
                CurrentProjectInfo.TagHelpersLoading = true;

                try
                {
                    tagHelpers = await snapshot.GetTagHelpersAsync();
                    CurrentProjectInfo.TagHelpers = new ObservableCollection<TagHelperViewModel>(tagHelpers.Select(t => new TagHelperViewModel(t)));
                }
                finally
                {
                    CurrentProjectInfo.TagHelpersLoading = false;
                }
            }
        }
    }
}
#endif