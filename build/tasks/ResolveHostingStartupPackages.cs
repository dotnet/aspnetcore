// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class ResolveHostingStartupPackages : Task
    {
        [Required]
        public ITaskItem[] BuildArtifacts { get; set; }

        [Required]
        public ITaskItem[] PackageArtifacts { get; set; }

        [Required]
        public ITaskItem[] ExternalDependencies { get; set; }

        [Output]
        public ITaskItem[] HostingStartupArtifacts { get; set; }

        public override bool Execute()
        {
            // Parse input
            var hostingStartupArtifacts = PackageArtifacts.Where(p => p.GetMetadata("HostingStartup") == "true");
            var externalHostingStartupArtifacts = ExternalDependencies.Where(p => p.GetMetadata("HostingStartup") == "true");

            var hostingStartups = BuildArtifacts.Where(p => hostingStartupArtifacts.Any(h => h.GetMetadata("Identity") == p.GetMetadata("PackageId")));

            foreach (var externalHostingStartup in externalHostingStartupArtifacts)
            {
                // The parameters PackageId and Version are required for output. For external dependencies, the identity is the pacakge id.
                externalHostingStartup.SetMetadata("PackageId", externalHostingStartup.GetMetadata("Identity"));
                hostingStartups = hostingStartups.Append(externalHostingStartup);
            }

            HostingStartupArtifacts = hostingStartups.ToArray();

            return true;
        }
    }
}
