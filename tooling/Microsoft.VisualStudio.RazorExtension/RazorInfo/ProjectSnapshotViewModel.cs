// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
            Properties = new ObservableCollection<PropertyViewModel>();

            InitializeProperties();
        }

        internal ProjectSnapshot Project { get; }

        public string Name => Path.GetFileNameWithoutExtension(Project.FilePath);

        public ProjectId Id { get; }

        public ObservableCollection<PropertyViewModel> Properties { get; }

        private void InitializeProperties()
        {
            Properties.Clear();

            Properties.Add(new PropertyViewModel("Language Version", Project.Configuration?.LanguageVersion.ToString()));
            Properties.Add(new PropertyViewModel("Configuration", FormatConfiguration(Project)));
            Properties.Add(new PropertyViewModel("Extensions", FormatExtensions(Project)));
            Properties.Add(new PropertyViewModel("Workspace Project", Project.WorkspaceProject?.Name));
        }

        private static string FormatConfiguration(ProjectSnapshot project)
        {
            return $"{project.Configuration.ConfigurationName} ({project.Configuration.GetType().Name})";
        }

        private static string FormatExtensions(ProjectSnapshot project)
        {
            return $"{string.Join(", ", project.Configuration.Extensions.Select(e => e.ExtensionName))}";
        }
    }
}
#endif