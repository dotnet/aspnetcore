// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Packaging;

namespace RepoTasks
{
    public class ReadPackageDependencies : Task
    {
        [Required]
        public ITaskItem[] PackageFiles { get; set; }

        [Output]
        public ITaskItem[] PackageDefinitions { get; set; }

        public override bool Execute()
        {
            PackageDefinitions = PackageFiles.SelectMany(item =>
            {
                using (var package = new PackageArchiveReader(item.ItemSpec))
                {
                    var identity = package.GetIdentity();
                    var metadata = new NuspecReader(package.GetNuspec());
                    var groups = metadata.GetDependencyGroups()?.ToList();
                    if (groups == null)
                    {
                        return Enumerable.Empty<ITaskItem>();
                    }

                    return groups.SelectMany(g =>
                        g.Packages.Select(p => new TaskItem(p.Id, new Hashtable { ["Version"] = p.VersionRange.MinVersion.ToString() })));
                }
            })
            .ToArray();

            return true;
        }
    }
}
