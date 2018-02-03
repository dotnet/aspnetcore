// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // All of the public state of this is immutable - we create a new instance and notify subscribers
    // when it changes. 
    //
    // However we use the private state to track things like dirty/clean.
    //
    // See the private constructors... When we update the snapshot we either are processing a Workspace
    // change (Project) or updating the computed state (ProjectSnapshotUpdateContext). We don't do both
    // at once. 
    internal class DefaultProjectSnapshot : ProjectSnapshot
    {
        public DefaultProjectSnapshot(HostProject hostProject, Project workspaceProject, VersionStamp? version = null)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            HostProject = hostProject;
            WorkspaceProject = workspaceProject; // Might be null
            
            FilePath = hostProject.FilePath;
            Version = version ?? VersionStamp.Default;
        }

        private DefaultProjectSnapshot(HostProject hostProject, DefaultProjectSnapshot other)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            HostProject = hostProject;

            ComputedVersion = other.ComputedVersion;
            FilePath = other.FilePath;
            WorkspaceProject = other.WorkspaceProject;

            Version = other.Version.GetNewerVersion();
        }

        private DefaultProjectSnapshot(Project workspaceProject, DefaultProjectSnapshot other)
        {
            if (workspaceProject == null)
            {
                throw new ArgumentNullException(nameof(workspaceProject));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            WorkspaceProject = workspaceProject;

            ComputedVersion = other.ComputedVersion;
            FilePath = other.FilePath;
            HostProject = other.HostProject;

            Version = other.Version.GetNewerVersion();
        }

        private DefaultProjectSnapshot(ProjectSnapshotUpdateContext update, DefaultProjectSnapshot other)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            ComputedVersion = update.Version;

            FilePath = other.FilePath;
            HostProject = other.HostProject;
            WorkspaceProject = other.WorkspaceProject;

            // This doesn't represent a new version of the underlying data. Keep the same version.
            Version = other.Version;
        }

        public override RazorConfiguration Configuration => HostProject.Configuration;

        public override string FilePath { get; }

        public HostProject HostProject { get; }

        public override bool IsInitialized => WorkspaceProject != null;

        public override VersionStamp Version { get; }

        public override Project WorkspaceProject { get; }

        // This is the version that the computed state is based on.
        public VersionStamp? ComputedVersion { get; set; }

        // We know the project is dirty if we don't have a computed result, or it was computed for a different version.
        // Since the PSM updates the snapshots synchronously, the snapshot can never be older than the computed state.
        public bool IsDirty => ComputedVersion == null || ComputedVersion.Value != Version;

        public ProjectSnapshotUpdateContext CreateUpdateContext()
        {
            return new ProjectSnapshotUpdateContext(FilePath, HostProject, WorkspaceProject, Version);
        }

        public DefaultProjectSnapshot WithHostProject(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            return new DefaultProjectSnapshot(hostProject, this);
        }

        public DefaultProjectSnapshot RemoveWorkspaceProject()
        {
            // We want to get rid of all of the computed state since it's not really valid.
            return new DefaultProjectSnapshot(HostProject, null, Version.GetNewerVersion());
        }

        public DefaultProjectSnapshot WithWorkspaceProject(Project workspaceProject)
        {
            if (workspaceProject == null)
            {
                throw new ArgumentNullException(nameof(workspaceProject));
            }

            return new DefaultProjectSnapshot(workspaceProject, this);
        }

        public DefaultProjectSnapshot WithComputedUpdate(ProjectSnapshotUpdateContext update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            return new DefaultProjectSnapshot(update, this);
        }

        public bool HasConfigurationChanged(DefaultProjectSnapshot original)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            // We don't have any computed state right now, so treat all background updates as
            // significant.
            return !object.Equals(ComputedVersion, original.ComputedVersion);
        }
    }
}