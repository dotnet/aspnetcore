// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace RepoTasks.ProjectModel
{
    [Flags]
    internal enum PatchPolicy
    {
        /// <summary>
        ///     Only produce new package versions if there were changes to product code.
        /// </summary>
        ProductChangesOnly = 1 << 0,

        /// <summary>
        ///     Packages should update in every patch.
        /// </summary>
        AlwaysUpdate = 1 << 1,

        /// <summary>
        ///     Produce new package versions if there were changes to product code, or if one of the package dependencies has updated.
        /// </summary>
        CascadeVersions = 1 << 2,

        AlwaysUpdateAndCascadeVersions = CascadeVersions | AlwaysUpdate,
    }
}
