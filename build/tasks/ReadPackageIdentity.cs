// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Packaging;

namespace RepoTasks
{
    public class ReadPackageIdentity : Task
    {
        [Required]
        public ITaskItem[] PackageFiles { get; set; }

        [Output]
        public ITaskItem[] PackageDefinitions { get; set; }

        public override bool Execute()
        {
            PackageDefinitions = PackageFiles.Select(item =>
            {
                using (var package = new PackageArchiveReader(item.ItemSpec))
                {
                    var identity = package.GetIdentity();
                    var packageItem = new TaskItem(identity.Id);
                    packageItem.SetMetadata("Version", identity.Version.ToString());
                    return packageItem;
                }
            })
            .ToArray();

            return true;
        }
    }
}
