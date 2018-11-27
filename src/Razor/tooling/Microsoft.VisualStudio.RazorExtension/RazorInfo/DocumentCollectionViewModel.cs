// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class DocumentCollectionViewModel : NotifyPropertyChanged
    {
        private readonly ProjectSnapshotManager _projectManager;
        private readonly Action<Exception> _errorHandler;

        private ProjectSnapshot _project;

        internal DocumentCollectionViewModel(ProjectSnapshotManager projectManager, ProjectSnapshot project, Action<Exception> errorHandler)
        {
            _projectManager = projectManager;
            _project = project;
            _errorHandler = errorHandler;

            Documents = new ObservableCollection<DocumentItemViewModel>();

            foreach (var filePath in project.DocumentFilePaths)
            {
                Documents.Add(new DocumentItemViewModel(projectManager, project.GetDocument(filePath), _errorHandler));
            }
        }

        public ObservableCollection<DocumentItemViewModel> Documents { get; }

        internal void OnChange(ProjectChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case ProjectChangeKind.DocumentAdded:
                    {
                        _project = _projectManager.GetLoadedProject(e.ProjectFilePath);
                        Documents.Add(new DocumentItemViewModel(_projectManager, _project.GetDocument(e.DocumentFilePath), _errorHandler));
                        break;
                    }

                case ProjectChangeKind.DocumentRemoved:
                    {
                        _project = _projectManager.GetLoadedProject(e.ProjectFilePath);

                        for (var i = Documents.Count - 1; i >= 0; i--)
                        {
                            if (Documents[i].FilePath == e.DocumentFilePath)
                            {
                                Documents.RemoveAt(i);
                                break;
                            }
                        }
                        
                        break;
                    }

                case ProjectChangeKind.DocumentChanged:
                    {
                        _project = _projectManager.GetLoadedProject(e.ProjectFilePath);
                        for (var i = Documents.Count - 1; i >= 0; i--)
                        {
                            if (Documents[i].FilePath == e.DocumentFilePath)
                            {
                                Documents[i] = new DocumentItemViewModel(_projectManager, _project.GetDocument(e.DocumentFilePath), _errorHandler);
                                break;
                            }
                        }

                        break;
                    }
            }
        }
    }
}

#endif
