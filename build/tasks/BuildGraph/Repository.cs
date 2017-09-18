// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;

namespace RepoTools.BuildGraph
{
    [DebuggerDisplay("{Name}")]
    public class Repository : IEquatable<Repository>
    {
        public Repository(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public string RootDir { get; set; }

        public IList<Project> Projects { get; } = new List<Project>();

        public IList<Project> SupportProjects { get; } = new List<Project>();

        public IEnumerable<Project> AllProjects => Projects.Concat(SupportProjects);

        public static IList<Repository> ReadAllRepositories(IList<string> repositoryPaths, DependencyGraphSpecProvider provider)
        {
            var repositories = new Repository[repositoryPaths.Count];

            Parallel.For(0, repositoryPaths.Count, new ParallelOptions { MaxDegreeOfParallelism = 6 }, i =>
            {
                var repositoryPath = repositoryPaths[i];
                var repositoryName = Path.GetFileName(repositoryPath);
                var repository = Read(provider, repositoryName, repositoryPath);
                repositories[i] = repository;
            });

            return repositories;
        }

        public bool Equals(Repository other) => string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

        private static Repository Read(DependencyGraphSpecProvider provider, string name, string repositoryPath)
        {
            var repository = new Repository(name);

            ReadSharedSourceProjects(Path.Combine(repositoryPath, "shared"), repository, repository.Projects);

            var srcDirectory = Path.GetFullPath(Path.Combine(repositoryPath, "src"))
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            var solutionFiles = Directory.EnumerateFiles(repositoryPath, "*.sln");
            foreach (var file in solutionFiles)
            {
                var spec = provider.GetDependencyGraphSpec(name, file);
                if (spec == null)
                {
                    continue;
                }

                var projects = spec.Projects.OrderBy(p => p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference ? 0 : 1);
                foreach (var specProject in projects)
                {
                    var projectPath = Path.GetFullPath(specProject.FilePath)
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    var projectGroup = projectPath.StartsWith(srcDirectory, StringComparison.OrdinalIgnoreCase) ?
                        repository.Projects :
                        repository.SupportProjects;

                    var project = projectGroup.FirstOrDefault(f => f.Path == specProject.FilePath);
                    if (project == null)
                    {
                        project = new Project(specProject.Name)
                        {
                            Repository = repository,
                            Path = specProject.FilePath,
                            Version = specProject.Version?.ToString(),
                        };

                        projectGroup.Add(project);
                    }

                    foreach (var package in GetPackageReferences(specProject))
                    {
                        project.PackageReferences.Add(package);
                    }
                }
            }

            return repository;
        }

        private static List<string> GetPackageReferences(PackageSpec specProject)
        {
            var allDependencies = Enumerable.Concat(
                specProject.Dependencies,
                specProject.TargetFrameworks.SelectMany(tfm => tfm.Dependencies))
                .Distinct();

            var packageReferences = allDependencies
                .Where(d => d.LibraryRange.TypeConstraintAllows(LibraryDependencyTarget.Package))
                .Select(d => d.Name)
                .ToList();
            return packageReferences;
        }

        private static void ReadSharedSourceProjects(string sharedSourceProjectsRoot, Repository repository, IList<Project> projects)
        {
            if (!Directory.Exists(sharedSourceProjectsRoot))
            {
                return;
            }

            foreach (var directory in new DirectoryInfo(sharedSourceProjectsRoot).EnumerateDirectories())
            {
                var project = new Project(directory.Name)
                {
                    Repository = repository,
                };
                projects.Add(project);
            }
        }
    }
}
