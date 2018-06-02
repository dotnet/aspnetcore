// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using NuGet.Frameworks;
using RepoTasks.Utilities;
using Microsoft.Build.Utilities;

namespace RepoTasks.ProjectModel
{
    internal class ProjectInfoFactory
    {
        private readonly TaskLoggingHelper _logger;

        public ProjectInfoFactory(TaskLoggingHelper logger)
        {
           _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ProjectInfo Create(string path, ProjectCollection projectCollection)
        {
            var project = GetProject(path, projectCollection);
            var instance = project.CreateProjectInstance(ProjectInstanceSettings.ImmutableWithFastItemLookup);

            var targetFrameworks = instance.GetPropertyValue("TargetFrameworks");
            var targetFramework = instance.GetPropertyValue("TargetFramework");

            var frameworks = new List<ProjectFrameworkInfo>();
            if (!string.IsNullOrEmpty(targetFrameworks) && string.IsNullOrEmpty(targetFramework))
            {
                // multi targeting
                foreach (var tfm in targetFrameworks.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    project.SetGlobalProperty("TargetFramework", tfm);
                    var innerBuild = project.CreateProjectInstance(ProjectInstanceSettings.ImmutableWithFastItemLookup);

                    var tfmInfo = new ProjectFrameworkInfo(NuGetFramework.Parse(tfm), GetDependencies(innerBuild));

                    frameworks.Add(tfmInfo);
                }

                project.RemoveGlobalProperty("TargetFramework");
            }
            else if (!string.IsNullOrEmpty(targetFramework))
            {
                var tfmInfo = new ProjectFrameworkInfo(NuGetFramework.Parse(targetFramework), GetDependencies(instance));

                frameworks.Add(tfmInfo);
            }

            var projectDir = Path.GetDirectoryName(path);

            var tools = GetTools(instance).ToArray();
            bool.TryParse(instance.GetPropertyValue("IsPackable"), out var isPackable);

            if (isPackable)
            {
                // the default packable setting is disabled for projects referencing this package.
                isPackable = !frameworks.SelectMany(f => f.Dependencies.Keys).Any(d => d.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
            }

            var packageId = instance.GetPropertyValue("PackageId");
            var packageVersion = instance.GetPropertyValue("PackageVersion");

            return new ProjectInfo(path,
                frameworks,
                tools,
                isPackable,
                packageId,
                packageVersion);
        }

        private static object _projLock = new object();

        private static Project GetProject(string path, ProjectCollection projectCollection)
        {
            var projects = projectCollection.GetLoadedProjects(path);
            foreach(var proj in projects)
            {
                if (proj.GetPropertyValue("DesignTimeBuild") == "true")
                {
                    return proj;
                }
            }

            var xml = ProjectRootElement.Open(path, projectCollection);
            var globalProps = new Dictionary<string, string>()
            {
                ["DesignTimeBuild"] = "true",
                 // Isolate the project from post-restore side effects
                ["ExcludeRestorePackageImports"] = "true",
            };

            var project = new Project(xml,
                globalProps,
                toolsVersion: "15.0",
                projectCollection: projectCollection)
            {
                IsBuildEnabled = false
            };

            return project;
        }

        private IReadOnlyDictionary<string, PackageReferenceInfo> GetDependencies(ProjectInstance project)
        {
            var references = new Dictionary<string, PackageReferenceInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in  project.GetItems("PackageReference"))
            {
                bool.TryParse(item.GetMetadataValue("IsImplicitlyDefined"), out var isImplicit);

                var info = new PackageReferenceInfo(item.EvaluatedInclude, item.GetMetadataValue("Version"), isImplicit);

                if (references.ContainsKey(info.Id))
                {
                    _logger.LogKoreBuildWarning(project.ProjectFileLocation.File, KoreBuildErrors.DuplicatePackageReference, $"Found a duplicate PackageReference for {info.Id}. Restore results may be unpredictable.");
                }

                references[info.Id] = info;
            }

            return references;
        }

        private static IEnumerable<DotNetCliReferenceInfo> GetTools(ProjectInstance project)
        {
            return project.GetItems("DotNetCliToolReference").Select(item =>
                new DotNetCliReferenceInfo(item.EvaluatedInclude, item.GetMetadataValue("Version")));
        }
    }
}
