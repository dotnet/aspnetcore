// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class DocumentSnapshotViewModel : NotifyPropertyChanged
    {
        internal DocumentSnapshotViewModel(DocumentSnapshot document)
        {
            Document = document;
        }

        internal DocumentSnapshot Document { get; }

        public string FilePath => Document.FilePath;

        public string TargetPath => Document.TargetPath;
    }
}
#endif