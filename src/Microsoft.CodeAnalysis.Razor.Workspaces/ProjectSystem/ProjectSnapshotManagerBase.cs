// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectSnapshotManagerBase : ProjectSnapshotManager
    {
        public abstract Workspace Workspace { get; }

        public abstract void ProjectUpdated(ProjectSnapshotUpdateContext update);

        public abstract void HostProjectAdded(HostProject hostProject);

        public abstract void HostProjectChanged(HostProject hostProject);

        public abstract void HostProjectRemoved(HostProject hostProject);

        public abstract void HostProjectBuildComplete(HostProject hostProject);

        public abstract void WorkspaceProjectAdded(Project workspaceProject);

        public abstract void WorkspaceProjectChanged(Project workspaceProject);

        public abstract void WorkspaceProjectRemoved(Project workspaceProject);

        public abstract void ReportError(Exception exception);

        public abstract void ReportError(Exception exception, ProjectSnapshot project);

        public abstract void ReportError(Exception exception, HostProject hostProject);

        public abstract void ReportError(Exception exception, Project workspaceProject);
    }
}