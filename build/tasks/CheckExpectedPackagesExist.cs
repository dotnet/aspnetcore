// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class CheckExpectedPackagesExist : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// The item group containing the nuget packages to split in different folders.
        /// </summary>
        [Required]
        public ITaskItem[] Packages { get; set; }

        [Required]
        public ITaskItem[] Files { get; set; }

        public override bool Execute()
        {
            if (Files?.Length == 0)
            {
                Log.LogError("No packages were found.");
                return false;
            }

            var expectedPackages = new HashSet<string>(Packages.Select(i => i.ItemSpec), StringComparer.OrdinalIgnoreCase);

            foreach (var file in Files)
            {
                PackageIdentity identity;
                using (var reader = new PackageArchiveReader(file.ItemSpec))
                {
                    identity = reader.GetIdentity();
                }

                if (!expectedPackages.Contains(identity.Id))
                {
                    Log.LogError($"Unexpected package artifact with id: {identity.Id}");
                    continue;
                }

                expectedPackages.Remove(identity.Id);
            }

            if (expectedPackages.Count != 0)
            {
                var error = new StringBuilder();
                foreach (var id in expectedPackages)
                {
                    error.Append(" - ").AppendLine(id);
                }

                Log.LogError($"Expected the following packages, but they were not found:" + error.ToString());
                return false;
            }

            return !Log.HasLoggedErrors;
        }
    }
}
