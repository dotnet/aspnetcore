// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Frameworks;
using NuGet.Versioning;
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
        public ITaskItem[] Artifacts { get; set; }

        // Artifacts that already shipped from repos
        [Required]
        public ITaskItem[] ShippedArtifacts { get; set; }

        [Required]
        public string Properties { get; set; }

        public string StartGraphAt { get; set; }

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
            var packageArtifacts = Artifacts.Select(ArtifactInfo.Parse)
                .OfType<ArtifactInfo.Package>()
                .Where(p => !p.IsSymbolsArtifact);

            var factory = new SolutionInfoFactory(Log, BuildEngine5);
            var props = MSBuildListSplitter.GetNamedProperties(Properties);

            Log.LogMessage(MessageImportance.High, $"Beginning cross-repo analysis on {Solutions.Length} solutions. Hang tight...");

            var solutions = factory.Create(Solutions, props, _cts.Token);
            Log.LogMessage($"Found {solutions.Count} and {solutions.Sum(p => p.Projects.Count)} projects");

            if (_cts.IsCancellationRequested)
            {
                return false;
            }

            EnsureConsistentGraph(packageArtifacts, solutions);
            RepositoryBuildOrder = GetRepositoryBuildOrder(packageArtifacts, solutions.Where(s => s.ShouldBuild));

            return !Log.HasLoggedErrors;
        }

        private struct VersionMismatch
        {
            public SolutionInfo Solution;
            public ProjectInfo Project;
            public string PackageId;
            public string ActualVersion;
            public NuGetVersion ExpectedVersion;
        }

        private void EnsureConsistentGraph(IEnumerable<ArtifactInfo.Package> packages, IEnumerable<SolutionInfo> solutions)
        {
             var shippedPackageMap = ShippedArtifacts
                .Select(ArtifactInfo.Parse)
                .OfType<ArtifactInfo.Package>()
                .Where(p => !p.IsSymbolsArtifact)
                .ToDictionary(p => p.PackageInfo.Id, p => p, StringComparer.OrdinalIgnoreCase);

            // ensure versions cascade
            var buildPackageMap = packages.ToDictionary(p => p.PackageInfo.Id, p => p, StringComparer.OrdinalIgnoreCase);

            var inconsistentVersions = new List<VersionMismatch>();
            var reposThatShouldPatch = new HashSet<string>();

            // holy crap, o^4
            foreach (var sln in solutions)
            foreach (var proj in sln.Projects)
            foreach (var tfm in proj.Frameworks)
            foreach (var dependency in tfm.Dependencies)
            {
                if (!buildPackageMap.TryGetValue(dependency.Key, out var package)) continue;

                var refVersion = VersionRange.Parse(dependency.Value.Version);
                if (refVersion.IsFloating && refVersion.Float.Satisfies(package.PackageInfo.Version))
                    continue;
                else if (package.PackageInfo.Version.Equals(refVersion))
                    continue;

                if (!sln.ShouldBuild)
                {
                    if (!shippedPackageMap.TryGetValue(proj.PackageId, out _))
                    {
                        Log.LogMessage(MessageImportance.Normal, $"Detected inconsistent in a sample or test project {proj.FullPath}");
                        continue;
                    }
                    else
                    {
                        reposThatShouldPatch.Add(Path.GetFileName(Path.GetDirectoryName(sln.FullPath)));
                    }
                }

                inconsistentVersions.Add(new VersionMismatch
                {
                    Solution = sln,
                    Project = proj,
                    PackageId = dependency.Key,
                    ActualVersion = dependency.Value.Version,
                    ExpectedVersion = package.PackageInfo.Version,
                });
            }

            if (inconsistentVersions.Count != 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine($"Repos are inconsistent. The following projects have PackageReferences that should be updated");
                foreach (var sln in inconsistentVersions.GroupBy(p => p.Solution.FullPath))
                {
                    sb.Append("  - ").AppendLine(Path.GetFileName(sln.Key));
                    foreach (var proj in sln.GroupBy(p => p.Project.FullPath))
                    {
                        sb.Append("      - ").AppendLine(Path.GetFileName(proj.Key));
                        foreach (var m in proj)
                        {
                            sb.AppendLine($"         + {m.PackageId}/{{{m.ActualVersion} => {m.ExpectedVersion}}}");
                        }
                    }
                }
                sb.AppendLine();
                Log.LogMessage(MessageImportance.High, sb.ToString());
                Log.LogError("Package versions are inconsistent. See build log for details.");
            }

            foreach (var repo in reposThatShouldPatch)
            {
                Log.LogError($"{repo} should not be a 'ShippedRepository'. Version changes in other repositories mean it should be patched to perserve cascading version upgrades.");
            }
        }

        private ITaskItem[] GetRepositoryBuildOrder(IEnumerable<ArtifactInfo.Package> artifacts, IEnumerable<SolutionInfo> solutions)
        {
            var repositories = solutions.Select(s =>
                {
                    var repoName = Path.GetFileName(Path.GetDirectoryName(s.FullPath));
                    var repo = new Repository(repoName);
                    var packages = artifacts.Where(a => a.RepoName.Equals(repoName, StringComparison.OrdinalIgnoreCase)).ToList();

                    foreach (var proj in s.Projects)
                    {
                        var projectGroup = packages.Any(p => p.PackageInfo.Id == proj.PackageId)
                            ? repo.Projects
                            : repo.SupportProjects;

                        projectGroup.Add(new Project(proj.PackageId)
                            {
                                Repository = repo,
                                PackageReferences = new HashSet<string>(proj.Frameworks.SelectMany(f => f.Dependencies.Keys), StringComparer.OrdinalIgnoreCase),
                            });
                    }

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
