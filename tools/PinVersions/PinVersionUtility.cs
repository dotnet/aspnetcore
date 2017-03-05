using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using UniverseTools;

namespace PinVersions
{
    class PinVersionUtility
    {
        private readonly string _repositoryRoot;
        private readonly FindPackageByIdResource[] _findPackageResources;
        private readonly ConcurrentDictionary<string, Task<NuGetVersion>> _exactMatches = new ConcurrentDictionary<string, Task<NuGetVersion>>(StringComparer.OrdinalIgnoreCase);
        private readonly DependencyGraphSpecProvider _provider;
        private readonly SourceCacheContext _sourceCacheContext;

        public PinVersionUtility(string repositoryRoot, List<string> pinSources, DependencyGraphSpecProvider provider)
        {
            _repositoryRoot = repositoryRoot;
            _findPackageResources = new FindPackageByIdResource[pinSources.Count];
            for (var i = 0; i < pinSources.Count; i++)
            {
                var repository = FactoryExtensionsV3.GetCoreV3(Repository.Factory, pinSources[i].Trim());
                _findPackageResources[i] = repository.GetResource<FindPackageByIdResource>();
            }
            _provider = provider;
            _sourceCacheContext = new SourceCacheContext();
        }

        public void Execute()
        {
            var repositoryDirectoryInfo = new DirectoryInfo(_repositoryRoot);
            var knownProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var slnFile in repositoryDirectoryInfo.EnumerateFiles("*.sln"))
            {
                var graphSpec = _provider.GetDependencyGraphSpec(repositoryDirectoryInfo.Name, slnFile.FullName);
                foreach (var specProject in graphSpec.Projects)
                {
                    if (!knownProjects.Add(specProject.FilePath) ||
                       specProject.RestoreMetadata.ProjectStyle != ProjectStyle.PackageReference)
                    {
                        continue;
                    }

                    var projectFileInfo = new FileInfo(specProject.FilePath);
                    var pinnedReferencesFile = Path.Combine(
                        specProject.RestoreMetadata.OutputPath,
                        projectFileInfo.Name + ".pinnedversions.targets");

                    Directory.CreateDirectory(Path.GetDirectoryName(pinnedReferencesFile));

                    var allDependencies = specProject.Dependencies.Select(dependency => new { Dependency = dependency, FrameworkName = NuGetFramework.AnyFramework })
                        .Concat(specProject.TargetFrameworks.SelectMany(tfm => tfm.Dependencies.Select(dependency => new { Dependency = dependency, tfm.FrameworkName })))
                        .Where(d => d.Dependency.LibraryRange.TypeConstraintAllows(LibraryDependencyTarget.Package));

                    var packageReferencesItemGroup = new XElement("ItemGroup");
                    foreach (var dependency in allDependencies)
                    {
                        var reference = dependency.Dependency;
                        var versionRange = reference.LibraryRange.VersionRange;
                        if (!versionRange.IsFloating)
                        {
                            continue;
                        }

                        var exactVersion = GetExactVersion(reference.Name, versionRange);
                        if (exactVersion == null)
                        {
                            continue;
                        }

                        var metadata = new List<XAttribute>
                        {
                            new XAttribute("Update", reference.Name),
                            new XAttribute("Version", exactVersion.ToNormalizedString()),
                        };

                        if (dependency.FrameworkName != NuGetFramework.AnyFramework)
                        {
                            metadata.Add(new XAttribute("Condition", $"'$(TargetFramework)'=='{dependency.FrameworkName.GetShortFolderName()}'"));
                        }

                        packageReferencesItemGroup.Add(new XElement("PackageReference", metadata));
                    }

                    var pinnedVersionRoot = new XElement("Project", packageReferencesItemGroup);
                    File.WriteAllText(pinnedReferencesFile, pinnedVersionRoot.ToString());
                }
            }
        }

        private NuGetVersion GetExactVersion(string name, VersionRange range)
        {
            if (range.MinVersion == null)
            {
                throw new Exception($"Unsupported version range {range}.");
            }

            if (!_exactMatches.TryGetValue(name, out var versionTask))
            {
                versionTask = _exactMatches.GetOrAdd(name, GetExactVersionAsync(name, range.MinVersion));
            }

            return versionTask.Result;
        }

        private async Task<NuGetVersion> GetExactVersionAsync(string name, NuGetVersion floatingVersion)
        {
            foreach (var findPackageResource in _findPackageResources)
            {
                var packageVersions = await findPackageResource.GetAllVersionsAsync(name, _sourceCacheContext, NullLogger.Instance, default(CancellationToken));

                var matchingVersions = packageVersions.Where(v => v.Version == floatingVersion.Version).ToList();
                switch (matchingVersions.Count)
                {
                    case 0:
                        return null;
                    case 1:
                        return matchingVersions[0];
                    default:
                        throw new Exception($"More than one version for {name} found that matches the specified version constraint: {string.Join(" ", matchingVersions)}.");
                }
            }

            return null;
        }
    }
}
