// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class DocumentItemViewModel : NotifyPropertyChanged
    {
        private readonly ProjectSnapshotManager _snapshotManager;
        private readonly DocumentSnapshot _document;
        private readonly Action<Exception> _errorHandler;

        private Visibility _progressVisibility;

        internal DocumentItemViewModel(ProjectSnapshotManager snapshotManager, DocumentSnapshot document, Action<Exception> errorHandler)
        {
            _snapshotManager = snapshotManager;
            _document = document;
            _errorHandler = errorHandler;

            InitializeGeneratedDocument();
        }

        public string FilePath => _document.FilePath;

        public string StatusText => _snapshotManager.IsDocumentOpen(_document.FilePath) ? "Open" : "Closed";

        public string TargetPath => _document.TargetPath;

        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set
            {
                _progressVisibility = value;
                OnPropertyChanged();
            }
        }

        private async void InitializeGeneratedDocument()
        {
            ProgressVisibility = Visibility.Hidden;

            try
            {
                if (!_document.TryGetGeneratedOutput(out var result))
                {
                    ProgressVisibility = Visibility.Visible;
                    await _document.GetGeneratedOutputAsync();
                    await Task.Delay(250); // Force a delay for the UI
                }
            }
            catch (Exception ex)
            {
                _errorHandler(ex);
            }
            finally
            {
                ProgressVisibility = Visibility.Hidden;
            }
        }
    }
}
#endif