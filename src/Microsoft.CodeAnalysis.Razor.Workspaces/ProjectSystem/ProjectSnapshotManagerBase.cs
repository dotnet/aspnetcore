// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectSnapshotManagerBase : ProjectSnapshotManager
    {
        public abstract Workspace Workspace { get; }

        public abstract void ProjectAdded(Project underlyingProject);

        public abstract void ProjectChanged(Project underlyingProject);

        public abstract void ProjectUpdated(ProjectSnapshotUpdateContext update);

        public abstract void ProjectRemoved(Project underlyingProject);

        public abstract void ProjectBuildComplete(Project underlyingProject);

        public abstract void ProjectsCleared();

        public abstract void ReportError(Exception exception);

        public abstract void ReportError(Exception exception, Project project);
    }
}
