// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace RepoTasks.ProjectModel
{
    internal class ProjectFrameworkInfo
    {
        public ProjectFrameworkInfo(NuGetFramework targetFramework, IReadOnlyDictionary<string, PackageReferenceInfo> dependencies)
        {
            TargetFramework = targetFramework ?? throw new ArgumentNullException(nameof(targetFramework));
            Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public NuGetFramework TargetFramework { get; }
        public IReadOnlyDictionary<string, PackageReferenceInfo> Dependencies { get; }
    }
}
