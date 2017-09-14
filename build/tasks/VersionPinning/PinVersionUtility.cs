// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace RepoTasks.VersionPinning
{
    internal class PinVersionUtility
    {
        private readonly string _repositoryRoot;
        private readonly FindPackageByIdResource[] _findPackageResources;
        private readonly ConcurrentDictionary<string, Task<NuGetVersion>> _exactMatches = new ConcurrentDictionary<string, Task<NuGetVersion>>(StringComparer.OrdinalIgnoreCase);
        private readonly DependencyGraphSpecProvider _provider;
        private readonly SourceCacheContext _sourceCacheContext;
        private readonly TaskLoggingHelper _logger;

        public PinVersionUtility(
            string repositoryRoot,
            List<string> pinSources,
            DependencyGraphSpecProvider provider,
            TaskLoggingHelper logger)
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
            _logger = logger;
        }

        public void Execute()
        {
            _logger.LogMessage(MessageImportance.High, $"Pinning package references for projects in {_repositoryRoot}");

            var solutionPinMetadata = GetProjectPinVersionMetadata();
            foreach (var cliToolReference in solutionPinMetadata.CLIToolReferences)
            {
                _logger.LogMessage(MessageImportance.Normal, $"Pinning CLI Tool {cliToolReference.Item1.Name}({cliToolReference.Item1.VersionRange} to {cliToolReference.Item2} for all projects in {_repositoryRoot}.");
            }

            foreach (var item in solutionPinMetadata.PinVersionLookup)
            {
                var projectPinMetadata = item.Value;
                var specProject = projectPinMetadata.PackageSpec;

                if (!(projectPinMetadata.Packages.Any() || solutionPinMetadata.CLIToolReferences.Any()))
                {
                    _logger.LogMessage(MessageImportance.Normal, $"No package or tool references to pin for {specProject.FilePath}.");
                    continue;
                }

                var projectFileInfo = new FileInfo(specProject.FilePath);
                var pinnedReferencesFile = Path.Combine(
                    specProject.RestoreMetadata.OutputPath,
                    projectFileInfo.Name + ".pinnedversions.targets");

                Directory.CreateDirectory(Path.GetDirectoryName(pinnedReferencesFile));

                if (projectPinMetadata.Packages.Any())
                {
                    _logger.LogMessage(MessageImportance.Normal, $"Pinning package versions for {specProject.FilePath}.");
                }

                var pinnedReferences = new XElement("ItemGroup", new XAttribute("Condition", "'$(PolicyDesignTimeBuild)' != 'true' AND !Exists('$(MSBuildThisFileDirectory)$(MSBuildProjectFile).nugetpolicy.g.targets')"));
                foreach (var packageReference in projectPinMetadata.Packages)
                {
                    (var tfm, var libraryRange, var exactVersion) = packageReference;
                    _logger.LogMessage(MessageImportance.Normal, $"Pinning reference {libraryRange.Name}({libraryRange.VersionRange} to {exactVersion}.");
                    var metadata = new List<XAttribute>
                    {
                        new XAttribute("Update", libraryRange.Name),
                        new XAttribute("Version", exactVersion.ToNormalizedString()),
                    };

                    if (tfm != NuGetFramework.AnyFramework)
                    {
                        metadata.Add(new XAttribute("Condition", $"'$(TargetFramework)'=='{tfm.GetShortFolderName()}'"));
                    }

                    pinnedReferences.Add(new XElement("PackageReference", metadata));
                }

                // CLI Tool references are specified at solution level.
                foreach (var toolReference in solutionPinMetadata.CLIToolReferences)
                {
                    (var libraryRange, var exactVersion) = toolReference;
                    var metadata = new List<XAttribute>
                    {
                        new XAttribute("Update", libraryRange.Name),
                        new XAttribute("Version", exactVersion.ToNormalizedString()),
                    };

                    pinnedReferences.Add(new XElement("DotNetCliToolReference", metadata));
                }

                var pinnedVersionRoot = new XElement("Project", pinnedReferences);
                File.WriteAllText(pinnedReferencesFile, pinnedVersionRoot.ToString());
            }
        }

        private SolutionPinVersionMetadata GetProjectPinVersionMetadata()
        {
            var repositoryDirectoryInfo = new DirectoryInfo(_repositoryRoot);
            var projects = new Dictionary<string, ProjectPinVersionMetadata>(StringComparer.OrdinalIgnoreCase);
            var cliToolReferences = new List<(LibraryRange, NuGetVersion)>();

            foreach (var slnFile in repositoryDirectoryInfo.EnumerateFiles("*.sln"))
            {
                var graphSpec = _provider.GetDependencyGraphSpec(repositoryDirectoryInfo.Name, slnFile.FullName);
                foreach (var specProject in graphSpec.Projects)
                {
                    if (!projects.TryGetValue(specProject.FilePath, out var pinMetadata))
                    {
                        pinMetadata = new ProjectPinVersionMetadata(specProject);
                        projects[specProject.FilePath] = pinMetadata;
                    }

                    var allDependencies = specProject.Dependencies.Select(dependency => new { Dependency = dependency, FrameworkName = NuGetFramework.AnyFramework })
                        .Concat(specProject.TargetFrameworks.SelectMany(tfm => tfm.Dependencies.Select(dependency => new { Dependency = dependency, tfm.FrameworkName })))
                        .Where(d => d.Dependency.LibraryRange.TypeConstraintAllows(LibraryDependencyTarget.Package));

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

                        var projectStyle = specProject.RestoreMetadata.ProjectStyle;
                        if (projectStyle == ProjectStyle.PackageReference)
                        {
                            pinMetadata.Packages.Add((dependency.FrameworkName, reference.LibraryRange, exactVersion));
                        }
                        else if (projectStyle == ProjectStyle.DotnetCliTool)
                        {
                            cliToolReferences.Add((reference.LibraryRange, exactVersion));
                        }
                        else
                        {
                            throw new NotSupportedException($"Unknown project style '{projectStyle}'.");
                        }
                    }
                }
            }

            return new SolutionPinVersionMetadata(projects, cliToolReferences);
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
                        continue;
                    case 1:
                        return matchingVersions[0];
                    default:
                        throw new Exception($"More than one version for {name} found that matches the specified version constraint: {string.Join(" ", matchingVersions)}.");
                }
            }

            return null;
        }

        private struct SolutionPinVersionMetadata
        {
            public SolutionPinVersionMetadata(
                IDictionary<string, ProjectPinVersionMetadata> pinVersionLookup,
                List<(LibraryRange, NuGetVersion)> cliToolReferences)
            {
                PinVersionLookup = pinVersionLookup;
                CLIToolReferences = cliToolReferences;
            }

            public IDictionary<string, ProjectPinVersionMetadata> PinVersionLookup { get; }

            public List<(LibraryRange, NuGetVersion)> CLIToolReferences { get; }
        }

        private struct ProjectPinVersionMetadata
        {
            public ProjectPinVersionMetadata(PackageSpec packageSpec)
            {
                PackageSpec = packageSpec;
                Packages = new List<(NuGetFramework, LibraryRange, NuGetVersion)>();
            }

            public PackageSpec PackageSpec { get; }

            public List<(NuGetFramework, LibraryRange, NuGetVersion)> Packages { get; }
        }
    }
}
