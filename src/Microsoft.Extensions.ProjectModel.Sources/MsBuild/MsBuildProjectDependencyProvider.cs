// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Execution;
using Microsoft.Extensions.ProjectModel.Resolution;

namespace Microsoft.Extensions.ProjectModel
{
    internal class MsBuildProjectDependencyProvider
    {
        private const string PackageDependencyItemType = "_DependenciesDesignTime";
        private const string ResolvedReferenceItemType = "ReferencePath";

        private readonly ProjectInstance _projectInstance;
        public MsBuildProjectDependencyProvider(ProjectInstance projectInstance)
        {
            if (projectInstance == null)
            {
                throw new ArgumentNullException(nameof(projectInstance));
            }
            _projectInstance = projectInstance;
        }

        public IEnumerable<DependencyDescription> GetPackageDependencies()
        {
            var packageItems = _projectInstance.GetItems(PackageDependencyItemType);
            var packageInfo = new Dictionary<string, DependencyDescription>(StringComparer.OrdinalIgnoreCase);
            if (packageItems != null)
            {
                foreach (var packageItem in packageItems)
                {
                    var packageDependency = CreateDependencyDescriptionFromItem(packageItem);
                    if (packageDependency != null)
                    {
                        packageInfo[packageItem.EvaluatedInclude] = packageDependency;
                    }
                }

                // 2nd pass to populate dependencies;

                PopulateDependencies(packageInfo, packageItems);
            }

            return packageInfo.Values;
        }


        public IEnumerable<ResolvedReference> GetResolvedReferences()
        {
            var refItems = _projectInstance.GetItems(ResolvedReferenceItemType);

            var resolvedReferences = refItems
                    ?.Select(refItem => CreateResolvedReferenceFromProjectItem(refItem))
                    .Where(resolvedReference => resolvedReference != null);

            return resolvedReferences;
        }

        private static ResolvedReference CreateResolvedReferenceFromProjectItem(ProjectItemInstance item)
        {
            var resolvedPath = item.EvaluatedInclude;

            if (string.IsNullOrEmpty(resolvedPath))
            {
                return null;
            }

            var name = Path.GetFileNameWithoutExtension(resolvedPath);
            return new ResolvedReference(name, resolvedPath);
        }

        private static DependencyDescription CreateDependencyDescriptionFromItem(ProjectItemInstance item)
        {
            // For type == Target, we do not get Name in the metadata. This is a special node where the dependencies are 
            // the direct dependencies of the project.
            var itemSpec = item.EvaluatedInclude;
            var name = item.HasMetadata("Name") ? item.GetMetadataValue("Name") : itemSpec;

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var version = item.GetMetadataValue("Version");
            var path = item.GetMetadataValue("Path");
            var type = item.GetMetadataValue("Type");
            var resolved = item.GetMetadataValue("Resolved");

            bool isResolved;
            isResolved = bool.TryParse(resolved, out isResolved) ? isResolved : false;
            var framework = itemSpec.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).First();

            return new DependencyDescription(name, version, path, framework, type, isResolved);
        }

        private static void PopulateDependencies(Dictionary<string, DependencyDescription> dependencies, ICollection<ProjectItemInstance> items)
        {
            foreach (var item in items)
            {
                var depSpecs = item.GetMetadataValue("Dependencies")
                    ?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                DependencyDescription currentDescription = null;
                if (depSpecs == null || !dependencies.TryGetValue(item.EvaluatedInclude, out currentDescription))
                {
                    return;
                }

                var prefix = item.EvaluatedInclude.Split('/').FirstOrDefault();
                foreach (var depSpec in depSpecs)
                {
                    var spec =  $"{prefix}/{depSpec}";
                    DependencyDescription dependency = null;
                    if (dependencies.TryGetValue(spec, out dependency))
                    {
                        var dep = new Dependency(dependency.Name, dependency.Version);
                        currentDescription.AddDependency(dep);
                    }
                }
            }
        }
    }
}
