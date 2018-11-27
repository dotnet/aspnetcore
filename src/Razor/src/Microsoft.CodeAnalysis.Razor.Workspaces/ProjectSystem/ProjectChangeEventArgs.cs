// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectChangeEventArgs : EventArgs
    {
        public ProjectChangeEventArgs(ProjectSnapshot older, ProjectSnapshot newer, ProjectChangeKind kind)
        {
            if (older == null && newer == null)
            {
                throw new ArgumentException("Both projects cannot be null.");
            }

            Older = older;
            Newer = newer;
            Kind = kind;

            ProjectFilePath = older?.FilePath ?? newer.FilePath;
        }

        public ProjectChangeEventArgs(ProjectSnapshot older, ProjectSnapshot newer, string documentFilePath, ProjectChangeKind kind)
        {
            if (older == null && newer == null)
            {
                throw new ArgumentException("Both projects cannot be null.");
            }

            Older = older;
            Newer = newer;
            DocumentFilePath = documentFilePath;
            Kind = kind;

            ProjectFilePath = older?.FilePath ?? newer.FilePath;
        }

        public ProjectSnapshot Older { get; }

        public ProjectSnapshot Newer { get; }

        public string ProjectFilePath { get; }

        public string DocumentFilePath { get; }

        public ProjectChangeKind Kind { get; }
    }
}
