// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class ProjectPropertyCollectionViewModel : NotifyPropertyChanged
    {
        private readonly ProjectSnapshot _project;

        internal ProjectPropertyCollectionViewModel(ProjectSnapshot project)
        {
            _project = project;
            
            Properties = new ObservableCollection<ProjectPropertyItemViewModel>();
            Properties.Add(new ProjectPropertyItemViewModel("Language Version", _project.Configuration?.LanguageVersion.ToString()));
            Properties.Add(new ProjectPropertyItemViewModel("Configuration", FormatConfiguration(_project)));
            Properties.Add(new ProjectPropertyItemViewModel("Extensions", FormatExtensions(_project)));
            Properties.Add(new ProjectPropertyItemViewModel("Workspace Project", _project.WorkspaceProject?.Name));
        }

        public ObservableCollection<ProjectPropertyItemViewModel> Properties { get; }

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
