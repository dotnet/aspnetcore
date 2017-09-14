// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class VerifyBuildGraph : Task
    {
        /// <summary>
        /// Repositories that we are building new versions of.
        /// </summary>
        [Required]
        public ITaskItem[] BuildRepositories { get; set; }

        /// <summary>
        /// Repos that have already been build and released. We don't compile and build them,
        /// but we still want to be sure their packages are accounted for in our graph calculations.
        /// </summary>
        public ITaskItem[] NoBuildRepositories { get; set; }

        /// <summary>
        /// New packages we are compiling. Used in the pin tool.
        /// </summary>
        [Output]
        public ITaskItem[] PackagesToBeProduced { get; set; }

        public override bool Execute()
        {
            return false;
        }
    }
}
