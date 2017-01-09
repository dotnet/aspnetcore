// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class ProjectViewModel : NotifyPropertyChanged
    {
        public ProjectViewModel(Project project)
        {
            Id = project.Id;
            Name = project.Name;
        }

        public string Name { get; }

        public ProjectId Id { get; }
    }
}
