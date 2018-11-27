// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class ProjectSnapshotViewModel : NotifyPropertyChanged
    {
        internal ProjectSnapshotViewModel(ProjectSnapshot project)
        {
            Project = project;

            Id = project.WorkspaceProject?.Id;
            Properties = new ObservableCollection<PropertyViewModel>()
            {
                new PropertyViewModel("Razor Language Version", project.Configuration?.LanguageVersion.ToString()),
                new PropertyViewModel("Configuration Name", $"{project.Configuration?.ConfigurationName} ({project.Configuration?.GetType().Name ?? "unknown"})"),
                new PropertyViewModel("Workspace Project", project.WorkspaceProject?.Name)
            };
        }

        internal ProjectSnapshot Project { get; }

        public string Name => Path.GetFileNameWithoutExtension(Project.FilePath);

        public ProjectId Id { get; }

        public ObservableCollection<PropertyViewModel> Properties { get; }
    }
}
#endif