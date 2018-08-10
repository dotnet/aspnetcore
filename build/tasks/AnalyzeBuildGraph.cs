// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Versioning;
using RepoTasks.ProjectModel;
using RepoTasks.Utilities;
using RepoTools.BuildGraph;

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
        public ITaskItem[] Repositories { get; set; }

        [Required]
        public ITaskItem[] Dependencies { get; set; }

        [Required]
        public string Properties { get; set; }

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

            var policies = new Dictionary<string, PatchPolicy>();
            foreach (var repo in Repositories)
            {
                policies.Add(repo.ItemSpec, Enum.Parse<PatchPolicy>(repo.GetMetadata("PatchPolicy")));
            }

            foreach (var solution in solutions)
            {
                var repoName = Path.GetFileName(solution.Directory);
                solution.PatchPolicy = policies[repoName];
            }

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
            var dependencyMap = new Dictionary<string, List<ExternalDependency>>(StringComparer.OrdinalIgnoreCase);
            foreach (var dep in Dependencies)
            {
                if (!dependencyMap.TryGetValue(dep.ItemSpec, out var versions))
                {
                    dependencyMap[dep.ItemSpec] = versions = new List<ExternalDependency>();
                }

                versions.Add(new ExternalDependency
                {
                    PackageId = dep.ItemSpec,
                    Version = dep.GetMetadata("Version"),
                });
            }

            var inconsistentVersions = new List<VersionMismatch>();

            foreach (var solution in solutions)
            foreach (var project in solution.Projects)
            foreach (var tfm in project.Frameworks)
            foreach (var dependency in tfm.Dependencies)
            {
                var dependencyVersion = dependency.Value.Version;
                if (!buildPackageMap.TryGetValue(dependency.Key, out var package))
                {
                    // This dependency is not one of the packages that will be compiled by this run of Universe.
                    // Must match an external dependency, including its Version.
                    var idx = -1;
                    if (!dependencyMap.TryGetValue(dependency.Key, out var externalVersions)
                        || (idx = externalVersions.FindIndex(0, externalVersions.Count, i => i.Version == dependencyVersion)) < 0)
                    {
                        Log.LogKoreBuildError(
                            project.FullPath,
                            KoreBuildErrors.UndefinedExternalDependency,
                            message: $"Undefined external dependency on {dependency.Key}/{dependencyVersion}");
                    }

                    if (idx >= 0)
                    {
                        externalVersions[idx].IsReferenced = true;
                    }
                    continue;
                }

                // This package will be created in this Universe run.
                var refVersion = VersionRange.Parse(dependencyVersion);
                if (refVersion.IsFloating && refVersion.Float.Satisfies(package.PackageInfo.Version))
                {
                    continue;
                }
                else if (package.PackageInfo.Version.Equals(refVersion.MinVersion))
                {
                    continue;
                }
                else if (dependencyMap.TryGetValue(dependency.Key, out var externalDependency) &&
                    externalDependency.Any(ext => ext.Version == dependencyVersion))
                {
                    // Project depends on external version of this package, not the version built in Universe. That's
                    // fine in benchmark apps for example.
                    continue;
                }

                var shouldCascade = (solution.PatchPolicy & PatchPolicy.CascadeVersions) != 0;
                if (!solution.ShouldBuild && !solution.IsPatching && shouldCascade)
                {
                    var repoName = Path.GetFileName(Path.GetDirectoryName(solution.FullPath));
                    Log.LogError($"{repoName} should not be marked 'IsPatching=false'. Version changes in other repositories mean it should be patched to perserve cascading version upgrades.");

                }

                if (shouldCascade)
                {
                    inconsistentVersions.Add(new VersionMismatch
                    {
                        Solution = solution,
                        Project = project,
                        PackageId = dependency.Key,
                        ActualVersion = dependency.Value.Version,
                        ExpectedVersion = package.PackageInfo.Version,
                    });
                }
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

            foreach (var versions in dependencyMap.Values)
            {
                foreach (var item in versions.Where(i => !i.IsReferenced))
                {
                    // See https://github.com/aspnet/Universe/wiki/Build-warning-and-error-codes#potentially-unused-external-dependency for details
                    Log.LogMessage(MessageImportance.Normal, $"Potentially unused external dependency: {item.PackageId}/{item.Version}. See https://github.com/aspnet/Universe/wiki/Build-warning-and-error-codes for details.");
                }
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

            var graph = GraphBuilder.Generate(repositories, Log);
            var repositoriesWithOrder = new List<(ITaskItem repository, int order)>();
            foreach (var repository in repositories)
            {
                var graphNodeRepository = graph.FirstOrDefault(g => g.Repository.Name == repository.Name);
                if (graphNodeRepository == null)
                {
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
