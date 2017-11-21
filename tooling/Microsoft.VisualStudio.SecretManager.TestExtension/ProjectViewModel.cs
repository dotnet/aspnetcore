// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.SecretManager.TestExtension
{
    public class ProjectViewModel : NotifyPropertyChanged
    {
        public ProjectViewModel(UnconfiguredProject project)
        {
            Project = project;
        }

        internal UnconfiguredProject Project { get; }

        public string ProjectName => Path.GetFileNameWithoutExtension(Project.FullPath);
    }
}
