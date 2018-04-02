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

        [Required]
        public ITaskItem[] Dependencies { get; set; }

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

            if (!props.TryGetValue("Configuration", out var defaultConfig))
            {
                defaultConfig = "Debug";
            }

            var solutions = factory.Create(Solutions, props, defaultConfig, _cts.Token);
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
            // ensure versions cascade
            var buildPackageMap = packages.ToDictionary(p => p.PackageInfo.Id, p => p, StringComparer.OrdinalIgnoreCase);
            var dependencyMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var dep in Dependencies)
            {
                if (!dependencyMap.TryGetValue(dep.ItemSpec, out var versions))
                {
                    dependencyMap[dep.ItemSpec] = versions = new List<string>();
                }

                versions.Add(dep.GetMetadata("Version"));
            }

            var inconsistentVersions = new List<VersionMismatch>();
            var reposThatShouldPatch = new HashSet<string>();

            // TODO cleanup the 4-deep nested loops
            foreach (var solution in solutions)
            foreach (var project in solution.Projects)
            foreach (var tfm in project.Frameworks)
            foreach (var dependency in tfm.Dependencies)
            {
                if (!buildPackageMap.TryGetValue(dependency.Key, out var package))
                {
                    // This dependency is not one of the packages that will be compiled by this run of Universe.
                    if (!dependencyMap.TryGetValue(dependency.Key, out var externalVersions)
                        || !externalVersions.Contains(dependency.Value.Version))
                    {
                        Log.LogKoreBuildError(
                            project.FullPath,
                            KoreBuildErrors.UndefinedExternalDependency,
                            message: $"Undefined external dependency on {dependency.Key}/{dependency.Value.Version}");
                    }
                    continue;
                }

                var refVersion = VersionRange.Parse(dependency.Value.Version);
                if (refVersion.IsFloating && refVersion.Float.Satisfies(package.PackageInfo.Version))
                {
                    continue;
                }
                else if (package.PackageInfo.Version.Equals(refVersion.MinVersion))
                {
                    continue;
                }

                if (!solution.ShouldBuild && solution.Shipped)
                {
                    reposThatShouldPatch.Add(Path.GetFileName(Path.GetDirectoryName(solution.FullPath)));
                }

                inconsistentVersions.Add(new VersionMismatch
                {
                    Solution = solution,
                    Project = project,
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
                foreach (var solution in inconsistentVersions.GroupBy(p => p.Solution.FullPath))
                {
                    sb.Append("  - ").AppendLine(Path.GetFileName(solution.Key));
                    foreach (var project in solution.GroupBy(p => p.Project.FullPath))
                    {
                        sb.Append("      - ").AppendLine(Path.GetFileName(project.Key));
                        foreach (var mismatchedReference in project)
                        {
                            sb.AppendLine($"         + {mismatchedReference.PackageId}/{{{mismatchedReference.ActualVersion} => {mismatchedReference.ExpectedVersion}}}");
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
                    var repo = new Repository(repoName)
                    {
                        RootDir = Path.GetDirectoryName(s.FullPath)
                    };

                    var packages = artifacts
                        .Where(a => string.Equals(a.RepoName, repoName, StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(p => p.PackageInfo.Id, p => p, StringComparer.OrdinalIgnoreCase);

                    foreach (var proj in s.Projects)
                    {
                        IList<Project> projectGroup;
                        if (packages.ContainsKey(proj.PackageId))
                        {
                            // this project is a package producer and consumer
                            packages.Remove(proj.PackageId);
                            projectGroup = repo.Projects;
                        }
                        else
                        {
                            // this project is a package consumer
                            projectGroup = repo.SupportProjects;
                        }


                        projectGroup.Add(new Project(proj.PackageId)
                            {
                                Repository = repo,
                                PackageReferences = new HashSet<string>(proj
                                    .Frameworks
                                    .SelectMany(f => f.Dependencies.Keys)
                                    .Concat(proj.Tools.Select(t => t.Id)), StringComparer.OrdinalIgnoreCase),
                            });
                    }

                    foreach (var packageId in packages.Keys)
                    {
                        // these packages are produced from something besides a csproj. e.g. .Sources packages
                        repo.Projects.Add(new Project(packageId) { Repository = repo });
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
                repositoryTaskItem.SetMetadata("RootPath", repository.RootDir);
                repositoriesWithOrder.Add((repositoryTaskItem, order));
            }

            return repositoriesWithOrder
                .OrderBy(r => r.order)
                .Select(r => r.repository)
                .ToArray();
        }
    }
}
