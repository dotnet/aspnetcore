// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using RepoTools.BuildGraph;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class CalculateBuildGraph : Task
    {
        [Required]
        public ITaskItem[] Repositories { get; set; }

        /// <summary>
        /// Directory that contains the package spec files.
        /// </summary>
        [Required]
        public string PackageSpecsDirectory { get; set; }

        /// <summary>
        /// Default to use for packages that may be produced from nuspec, not csproj. (e.g. .Sources packages).
        /// </summary>
        [Required]
        public string DefaultPackageVersion { get; set; }

        /// <summary>
        /// The repository at which to root the graph at
        /// </summary>
        public string StartGraphAt { get; set; }

        [Output]
        public ITaskItem[] RepositoriesToBuildInOrder { get; set; }

        [Output]
        public ITaskItem[] PackagesProduced { get; set; }

        public override bool Execute()
        {
            var graphSpecProvider = new DependencyGraphSpecProvider(PackageSpecsDirectory.Trim());

            var repositoryPaths = Repositories.Select(r => r.GetMetadata("RepositoryPath")).ToList();
            var repositories = Repository.ReadAllRepositories(repositoryPaths, graphSpecProvider);

            var graph = GraphBuilder.Generate(repositories, StartGraphAt, Log);
            var repositoriesWithOrder = new List<(ITaskItem repository, int order)>();
            foreach (var repositoryTaskItem in Repositories)
            {
                var repositoryName = repositoryTaskItem.ItemSpec;
                var graphNodeRepository = graph.First(g => g.Repository.Name == repositoryName);
                var order = TopologicalSort.GetOrder(graphNodeRepository);
                repositoryTaskItem.SetMetadata("Order", order.ToString());
                repositoriesWithOrder.Add((repositoryTaskItem, order));
            }

            Log.LogMessage(MessageImportance.High, "Repository build order:");
            foreach (var buildGroup in repositoriesWithOrder.GroupBy(r => r.order).OrderBy(g => g.Key))
            {
                var buildGroupRepos = buildGroup.Select(b => b.repository.ItemSpec);
                Log.LogMessage(MessageImportance.High, $"{buildGroup.Key.ToString().PadLeft(2, ' ')}: {string.Join(", ", buildGroupRepos)}");
            }

            RepositoriesToBuildInOrder = repositoriesWithOrder
                .OrderBy(r => r.order)
                .Select(r => r.repository)
                .ToArray();

            var packages = new List<ITaskItem>();
            foreach (var project in repositories.SelectMany(p => p.Projects))
            {
                var pkg = new TaskItem(project.Name);
                var version = project.Version ?? DefaultPackageVersion;
                pkg.SetMetadata("Version", version);
                packages.Add(pkg);
            }

            PackagesProduced = packages.ToArray();

            return true;
        }
    }
}
