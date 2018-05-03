// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class DocumentSnapshotViewModel : NotifyPropertyChanged
    {
        private double _progress;

        internal DocumentSnapshotViewModel(DocumentSnapshot document)
        {
            Document = document;

            InitializeGeneratedDocument();
        }

        internal DocumentSnapshot Document { get; }

        public string FilePath => Document.FilePath;

        public string TargetPath => Document.TargetPath;

        public bool CodeGenerationInProgress => _progress < 100;

        public double CodeGenerationProgress => _progress;

        private async void InitializeGeneratedDocument()
        {
            _progress = 0;
            OnPropertyChanged(nameof(CodeGenerationInProgress));
            OnPropertyChanged(nameof(CodeGenerationProgress));

            try
            {
                await Document.GetGeneratedOutputAsync();
            }
            finally
            {
                _progress = 100;
                OnPropertyChanged(nameof(CodeGenerationInProgress));
                OnPropertyChanged(nameof(CodeGenerationProgress));
            }
        }
    }
}
#endif