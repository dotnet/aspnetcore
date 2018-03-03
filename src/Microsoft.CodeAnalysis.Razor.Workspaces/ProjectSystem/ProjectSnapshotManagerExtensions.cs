// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal static class ProjectSnapshotManagerExtensions
    {
        public static ProjectSnapshot GetProjectWithFilePath(this ProjectSnapshotManager snapshotManager, string filePath)
        {
            var projects = snapshotManager.Projects;
            for (var i = 0; i< projects.Count; i++)
            {
                var project = projects[i];
                if (FilePathComparer.Instance.Equals(filePath, project.FilePath))
                {
                    return project;
                }
            }

            return null;
        }
    }
}
