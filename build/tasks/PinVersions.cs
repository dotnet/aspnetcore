// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using RepoTasks.VersionPinning;

namespace RepoTasks
{
    public class PinVersions : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string BuildRepositoryRoot { get; set; }

        [Required]
        public ITaskItem[] PackageSources { get; set; }

        public string GraphSpecsRoot { get; set; }

        public override bool Execute()
        {
            if (PackageSources?.Length == 0)
            {
                Log.LogError($"Missing PackageSources. At least one item source must be specified.");
                return false;
            }

            var graphSpecProvider = !string.IsNullOrEmpty(GraphSpecsRoot)
                ? new DependencyGraphSpecProvider(GraphSpecsRoot)
                : DependencyGraphSpecProvider.Default;

            using (graphSpecProvider)
            {
                var pinVersionUtility = new PinVersionUtility(
                    BuildRepositoryRoot,
                    PackageSources.Select(i => i.ItemSpec).ToList(),
                    graphSpecProvider,
                    Log);
                pinVersionUtility.Execute();
            }

            return true;
        }
    }
}
