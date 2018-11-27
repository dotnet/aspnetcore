// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    internal class RazorInfoViewModel : NotifyPropertyChanged
    {
        private readonly Workspace _workspace;
        private readonly ProjectSnapshotManager _projectManager;
        private readonly Action<Exception> _errorHandler;
        
        private ProjectViewModel _selectedProject;
        private ProjectPropertyCollectionViewModel _projectProperties;
        private DirectiveCollectionViewModel _directives;
        private DocumentCollectionViewModel _documents;
        private TagHelperCollectionViewModel _tagHelpers;
        private ICommand _updateCommand;

        public RazorInfoViewModel(Workspace workspace, ProjectSnapshotManager projectManager, Action<Exception> errorHandler)
        {
            _workspace = workspace;
            _projectManager = projectManager;
            _errorHandler = errorHandler;
            
            UpdateCommand = new RelayCommand<object>(ExecuteUpdate, CanExecuteUpdate);
        }

        public ObservableCollection<ProjectViewModel> Projects { get; } = new ObservableCollection<ProjectViewModel>();

        public ProjectViewModel SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                _selectedProject = value;

                OnPropertyChanged();
                OnSelectedProjectChanged();
            }
        }

        public ProjectPropertyCollectionViewModel ProjectProperties
        {
            get { return _projectProperties; }
            set
            {
                _projectProperties = value;
                OnPropertyChanged();
            }
        }

        public DirectiveCollectionViewModel Directives
        {
            get { return _directives; }
            set
            {
                _directives = value;
                OnPropertyChanged();
            }
        }

        public DocumentCollectionViewModel Documents
        {
            get { return _documents; }
            set
            {
                _documents = value;
                OnPropertyChanged();
            }
        }

        public TagHelperCollectionViewModel TagHelpers
        {
            get { return _tagHelpers; }
            set
            {
                _tagHelpers = value;
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

        public void OnChange(ProjectChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case ProjectChangeKind.ProjectAdded:
                    {
                        var added = new ProjectViewModel(e.ProjectFilePath);
                        Projects.Add(added);

                        if (Projects.Count == 1)
                        {
                            SelectedProject = added;
                        }

                        break;
                    }

                case ProjectChangeKind.ProjectRemoved:
                    {
                        ProjectViewModel removed = null;
                        for (var i = Projects.Count - 1; i >= 0; i--)
                        {
                            var project = Projects[i];
                            if (project.FilePath == e.ProjectFilePath)
                            {
                                removed = project;
                                Projects.RemoveAt(i);
                                break;
                            }
                        }

                        if (SelectedProject == removed)
                        {
                            SelectedProject = null;
                        }

                        break;
                    }

                case ProjectChangeKind.ProjectChanged:
                    {
                        if (SelectedProject != null && SelectedProject.FilePath == e.ProjectFilePath)
                        {
                            OnSelectedProjectChanged();
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentAdded:
                case ProjectChangeKind.DocumentRemoved:
                case ProjectChangeKind.DocumentChanged:
                    {
                        if (SelectedProject != null && SelectedProject.FilePath == e.ProjectFilePath)
                        {
                            Documents?.OnChange(e);
                        }

                        break;
                    }
            }
        }

        private void OnSelectedProjectChanged()
        {
            if (SelectedProject == null)
            {
                ProjectProperties = null;
                Directives = null;
                Documents = null;
                TagHelpers = null;

                return;
            }

            var project = _projectManager.GetLoadedProject(_selectedProject.FilePath);
            ProjectProperties = new ProjectPropertyCollectionViewModel(project);
            Directives = new DirectiveCollectionViewModel(project);
            Documents = new DocumentCollectionViewModel(_projectManager, project, _errorHandler);
            TagHelpers = new TagHelperCollectionViewModel(project, _errorHandler);
        }

        private bool CanExecuteUpdate(object state)
        {
            return SelectedProject != null;
        }

        private void ExecuteUpdate(object state)
        {
            var projectFilePath = SelectedProject?.FilePath;
            if (projectFilePath == null)
            {
                return;
            }

            var project = _projectManager.GetLoadedProject(projectFilePath);
            if (project != null && project.WorkspaceProject != null)
            {
                var solution = _workspace.CurrentSolution;
                var workspaceProject = solution.GetProject(project.WorkspaceProject.Id);
                if (workspaceProject != null)
                {
                    ((ProjectSnapshotManagerBase)_projectManager).WorkspaceProjectChanged(workspaceProject);
                }
            }
        }
    }
}

#endif