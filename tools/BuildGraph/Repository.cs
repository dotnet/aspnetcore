using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using UniverseTools;

namespace BuildGraph
{
    [DebuggerDisplay("{Name}")]
    public class Repository : IEquatable<Repository>
    {
        public Repository(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public IList<Project> Projects { get; } = new List<Project>();

        public IList<Project> SupportProjects { get; } = new List<Project>();

        public IEnumerable<Project> AllProjects => Projects.Concat(SupportProjects);

        public static IList<Repository> ReadAllRepositories(string repositoriesRoot, DependencyGraphSpecProvider provider)
        {
            var directories = new DirectoryInfo(repositoriesRoot).GetDirectories();
            var repositories = new Repository[directories.Length];

            var sw = Stopwatch.StartNew();
            Parallel.For(0, directories.Length, new ParallelOptions { MaxDegreeOfParallelism = 6 }, i =>
            {
                var directoryInfo = directories[i];
                Console.WriteLine($"Gathering dependency information from {directoryInfo.Name}.");

                var repository = Read(provider, directoryInfo.Name, directoryInfo.FullName);
                repositories[i] = repository;

                Console.WriteLine($"Done gathering dependency information from {directoryInfo.Name}.");
            });
            sw.Stop();

            Console.WriteLine($"Done reading dependency information for all repos in {sw.Elapsed}.");

            return repositories;
        }

        public bool Equals(Repository other) => string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

        private static Repository Read(DependencyGraphSpecProvider provider, string name, string repositoryPath)
        {
            var repository = new Repository(name);

            ReadSharedSourceProjects(Path.Combine(repositoryPath, "shared"), repository, repository.Projects);
            var srcDirectory = Path.Combine(repositoryPath, "src");

            var solutionFiles = Directory.EnumerateFiles(repositoryPath, "*.sln");
            var knownProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in solutionFiles)
            {
                var spec = provider.GetDependencyGraphSpec(name, file);
                foreach (var specProject in spec.Projects)
                {
                    if (!knownProjects.Add(specProject.FilePath) ||
                        specProject.RestoreMetadata.ProjectStyle != ProjectStyle.PackageReference)
                    {
                        continue;
                    }

                    var projectPath = Path.GetFullPath(specProject.FilePath);

                    var project = new Project(specProject.Name)
                    {
                        PackageReferences = GetPackageReferences(specProject),
                        Repository = repository,
                    };

                    var projectGroup = projectPath.StartsWith(srcDirectory, StringComparison.OrdinalIgnoreCase) ?
                        repository.Projects :
                        repository.SupportProjects;
                    projectGroup.Add(project);
                }
            }

            return repository;
        }

        private static List<string> GetPackageReferences(NuGet.ProjectModel.PackageSpec specProject)
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