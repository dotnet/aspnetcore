// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectChangeEventArgs : EventArgs
    {
        public ProjectChangeEventArgs(string projectFilePath, ProjectChangeKind kind)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            ProjectFilePath = projectFilePath;
            Kind = kind;
        }

        public ProjectChangeEventArgs(string projectFilePath, string documentFilePath, ProjectChangeKind kind)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            ProjectFilePath = projectFilePath;
            DocumentFilePath = documentFilePath;
            Kind = kind;
        }

        public string ProjectFilePath { get; }

        public string DocumentFilePath { get; }

        public ProjectChangeKind Kind { get; }
    }
}
