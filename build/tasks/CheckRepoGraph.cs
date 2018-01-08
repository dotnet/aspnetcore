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
using NuGet.Packaging.Core;
using NuGet.Versioning;
using RepoTools.BuildGraph;
using RepoTasks.ProjectModel;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class CheckRepoGraph : Task, ICancelableTask
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [Required]
        public ITaskItem[] Solutions { get; set; }

        [Required]
        public ITaskItem[] Artifacts { get; set; }

        [Required]
        public ITaskItem[] Repositories { get; set; }

        [Required]
        public string Properties { get; set; }

        public void Cancel()
        {
            _cts.Cancel();
        }

        public override bool Execute()
        {
            var packageArtifacts = Artifacts.Select(ArtifactInfo.Parse)
                .OfType<ArtifactInfo.Package>()
                .Where(p => !p.IsSymbolsArtifact)
                .ToDictionary(p => p.PackageInfo.Id, p => p, StringComparer.OrdinalIgnoreCase);

            var factory = new SolutionInfoFactory(Log, BuildEngine5);
            var props = MSBuildListSplitter.GetNamedProperties(Properties);

            if (!props.TryGetValue("Configuration", out var defaultConfig))
            {
                defaultConfig = "Debug";
            }

            var solutions = factory.Create(Solutions, props, defaultConfig, _cts.Token).OrderBy(f => f.Directory).ToList();
            Log.LogMessage($"Found {solutions.Count} and {solutions.Sum(p => p.Projects.Count)} projects");

            if (_cts.IsCancellationRequested)
            {
                return false;
            }

            var repoGraph = new AdjacencyMatrix(solutions.Count);
            var packageToProjectMap = new Dictionary<PackageIdentity, ProjectInfo>();

            for (var i = 0; i < solutions.Count; i++)
            {
                var sln = repoGraph[i] = solutions[i];

                foreach (var proj in sln.Projects)
                {
                    if (!proj.IsPackable
                        || proj.FullPath.Contains("samples")
                        || proj.FullPath.Contains("tools/Microsoft.VisualStudio.Web.CodeGeneration.Design"))
                    {
                        continue;
                    }

                    var id = new PackageIdentity(proj.PackageId, new NuGetVersion(proj.PackageVersion));

                    if (packageToProjectMap.TryGetValue(id, out var otherProj))
                    {
                        Log.LogError($"Both {proj.FullPath} and {otherProj.FullPath} produce {id}");
                        continue;
                    }

                    packageToProjectMap.Add(id, proj);
                }

                var sharedSrc = Path.Combine(sln.Directory, "shared");
                if (Directory.Exists(sharedSrc))
                {
                    foreach (var dir in Directory.GetDirectories(sharedSrc, "*.Sources"))
                    {
                        var id = GetDirectoryName(dir);
                        var artifactInfo = packageArtifacts[id];
                        var sharedSrcProj = new ProjectInfo(dir,
                            Array.Empty<ProjectFrameworkInfo>(),
                            Array.Empty<DotNetCliReferenceInfo>(),
                            true,
                            artifactInfo.PackageInfo.Id,
                            artifactInfo.PackageInfo.Version.ToNormalizedString());
                        sharedSrcProj.SolutionInfo = sln;
                        var identity = new PackageIdentity(artifactInfo.PackageInfo.Id, artifactInfo.PackageInfo.Version);
                        packageToProjectMap.Add(identity, sharedSrcProj);
                    }
                }
            }

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            for (var i = 0; i < solutions.Count; i++)
            {
                var src = repoGraph[i];

                foreach (var proj in src.Projects)
                {
                    if (!proj.IsPackable
                        || proj.FullPath.Contains("samples"))
                    {
                        continue;
                    }

                    foreach (var dep in proj.Frameworks.SelectMany(f => f.Dependencies.Values))
                    {
                        if (packageToProjectMap.TryGetValue(new PackageIdentity(dep.Id, new NuGetVersion(dep.Version)), out var target))
                        {
                            var j = repoGraph.FindIndex(target.SolutionInfo);
                            repoGraph.SetLink(i, j);
                        }
                    }

                    foreach (var toolDep in proj.Tools)
                    {
                        if (packageToProjectMap.TryGetValue(new PackageIdentity(toolDep.Id, new NuGetVersion(toolDep.Version)), out var target))
                        {
                            var j = repoGraph.FindIndex(target.SolutionInfo);
                            repoGraph.SetLink(i, j);
                        }
                    }
                }
            }

            var repos = Repositories.ToDictionary(i => i.ItemSpec, i => i, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < repoGraph.Count; i++)
            {
                var src = repoGraph[i];
                var repoName = GetDirectoryName(src.Directory);
                var repo = repos[repoName];

                for (var j = 0; j < repoGraph.Count; j++)
                {
                    if (j == i) continue;
                    if (repoGraph.HasLink(i, j))
                    {
                        var target = repoGraph[j];
                        var targetRepoName = GetDirectoryName(target.Directory);
                        var targetRepo = repos[targetRepoName];

                        if (src.Shipped && !target.Shipped)
                        {
                            Log.LogError($"{repoName} cannot depend on {targetRepoName}. Repos marked as 'Shipped' cannot depend on repos that are rebuilding. Update the configuration in submodule.props.");
                        }
                    }
                }
            }

            return !Log.HasLoggedErrors;
        }

        private static string GetDirectoryName(string path)
            => Path.GetFileName(path.TrimEnd(new[] { '\\', '/' }));

        private class AdjacencyMatrix
        {
            private readonly bool[,] _matrix;
            private readonly SolutionInfo[] _items;

            public AdjacencyMatrix(int size)
            {
                _matrix = new bool[size, size];
                _items = new SolutionInfo[size];
                Count = size;
            }

            public SolutionInfo this[int idx]
            {
                get => _items[idx];
                set => _items[idx] = value;
            }

            public int FindIndex(SolutionInfo item)
            {
                return Array.FindIndex(_items, t => t.Equals(item));
            }

            public int Count { get; }

            public bool HasLink(int source, int target) => _matrix[source, target];

            public void SetLink(int source, int target)
            {
                _matrix[source, target] = true;
            }
        }
    }
}
