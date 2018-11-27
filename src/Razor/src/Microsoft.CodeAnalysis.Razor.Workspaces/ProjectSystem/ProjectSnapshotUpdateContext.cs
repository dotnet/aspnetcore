// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectSnapshotUpdateContext
    {
        public ProjectSnapshotUpdateContext(string filePath, HostProject hostProject, Project workspaceProject, VersionStamp version)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            if (workspaceProject == null)
            {
                throw new ArgumentNullException(nameof(workspaceProject));
            }

            FilePath = filePath;
            HostProject = hostProject;
            WorkspaceProject = workspaceProject;
            Version = version;
        }

        public string FilePath { get; }

        public HostProject HostProject { get; }

        public Project WorkspaceProject { get; }
        
        public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; set; }

        public VersionStamp Version { get; }
    }
}
