// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RepoTools.BuildGraph;
using RepoTasks.ProjectModel;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class AnalyzeBuildGraph : Task, ICancelableTask
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Repositories that we are building new versions of.
        /// </summary>
        [Required]
        public ITaskItem[] Solutions { get; set; }

        [Required]
        public string Properties { get; set; }

        public string StartGraphAt { get; set; }

        /// <summary>
        /// New packages we are compiling. Used in the pin tool.
        /// </summary>
        [Output]
        public ITaskItem[] PackagesProduced { get; set; }

        /// <summary>
        /// The order in which to build repositories
        /// </summary>
        [Output]
        public ITaskItem[] RepositoryBuildOrder { get; set; }

        public void Cancel()
        {
            _cts.Cancel();
        }

        public override bool Execute()
        {
            var factory = new SolutionInfoFactory(Log, BuildEngine5);
            var props = MSBuildListSplitter.GetNamedProperties(Properties);

            Log.LogMessage(MessageImportance.High, $"Beginning cross-repo analysis on {Solutions.Length} solutions. Hang tight...");

            var solutions = factory.Create(Solutions, props, _cts.Token);
            Log.LogMessage($"Found {solutions.Count} and {solutions.Sum(p => p.Projects.Count)} projects");

            if (_cts.IsCancellationRequested)
            {
                return false;
            }

            EnsureConsistentGraph(solutions);
            PackagesProduced = GetPackagesProduced(solutions);
            RepositoryBuildOrder = GetRepositoryBuildOrder(solutions.Where(s => s.ShouldBuild));

            return !Log.HasLoggedErrors;
        }

        private void EnsureConsistentGraph(IEnumerable<SolutionInfo> solutions)
        {
            // TODO
        }

        private ITaskItem[] GetPackagesProduced(IEnumerable<SolutionInfo> solutions)
        {
            return solutions
                .Where(s => s.ShouldBuild)
                .SelectMany(p => p.Projects)
                .Where(p => p.IsPackable)
                .Select(p => new TaskItem(p.PackageId, new Hashtable
                {
                    ["Version"] = p.PackageVersion
                }))
                .ToArray();
        }

        private ITaskItem[] GetRepositoryBuildOrder(IEnumerable<SolutionInfo> solutions)
        {
            var repositories = solutions.Select(s =>
                {
                    var repoName = Path.GetFileName(Path.GetDirectoryName(s.FullPath));
                    var repo = new Repository(repoName);
                    repo.Projects = s.Projects
                            .Where(p => p.IsPackable)
                            .Select(p =>
                                new Project(p.PackageId)
                                {
                                    Repository = repo,
                                    PackageReferences = new HashSet<string>(p.Frameworks.SelectMany(f => f.Dependencies.Keys), StringComparer.OrdinalIgnoreCase),
                                })
                            .ToList();
                    repo.SupportProjects = s.Projects
                            .Where(p => !p.IsPackable)
                            .Select(p =>
                                new Project(p.PackageId)
                                {
                                    Repository = repo,
                                    PackageReferences = new HashSet<string>(p.Frameworks.SelectMany(f => f.Dependencies.Keys), StringComparer.OrdinalIgnoreCase),
                                })
                            .ToList();
                    return repo;
                }).ToList();

            var graph = GraphBuilder.Generate(repositories, StartGraphAt, Log);
            var repositoriesWithOrder = new List<(ITaskItem repository, int order)>();
            foreach (var repository in repositories)
            {
                var graphNodeRepository = graph.FirstOrDefault(g => g.Repository.Name == repository.Name);
                if (graphNodeRepository == null)
                {
                    // StartGraphAt was specified so the graph is incomplete.
                    continue;
                }

                var order = TopologicalSort.GetOrder(graphNodeRepository);
                var repositoryTaskItem = new TaskItem(repository.Name);
                repositoryTaskItem.SetMetadata("Order", order.ToString());
                repositoriesWithOrder.Add((repositoryTaskItem, order));
            }

            Log.LogMessage(MessageImportance.High, "Repository build order:");
            foreach (var buildGroup in repositoriesWithOrder.GroupBy(r => r.order).OrderBy(g => g.Key))
            {
                var buildGroupRepos = buildGroup.Select(b => b.repository.ItemSpec);
                Log.LogMessage(MessageImportance.High, $"{buildGroup.Key.ToString().PadLeft(2, ' ')}: {string.Join(", ", buildGroupRepos)}");
            }

            return repositoriesWithOrder
                .OrderBy(r => r.order)
                .Select(r => r.repository)
                .ToArray();
        }
    }
}
