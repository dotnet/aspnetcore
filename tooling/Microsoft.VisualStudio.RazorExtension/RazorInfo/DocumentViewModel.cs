// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class DocumentViewModel : NotifyPropertyChanged
    {
        public DocumentViewModel(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }
}