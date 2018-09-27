// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;

namespace RepoTools.BuildGraph
{
    public static class GraphBuilder
    {
        public static IList<GraphNode> Generate(IList<Repository> repositories, TaskLoggingHelper log)
        {
            // Build global list of primary projects
            var primaryProjects = repositories.SelectMany(c => c.Projects)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            var graphNodes = repositories.Select(r => new GraphNode { Repository = r })
                .ToDictionary(r => r.Repository);

            foreach (var project in repositories.SelectMany(r => r.AllProjects))
            {
                var thisProjectRepositoryNode = graphNodes[project.Repository];

                foreach (var packageDependency in project.PackageReferences)
                {
                    if (primaryProjects.TryGetValue(packageDependency, out var dependencyProject))
                    {
                        var dependencyRepository = dependencyProject.Repository;
                        var dependencyNode = graphNodes[dependencyRepository];

                        if (ReferenceEquals(thisProjectRepositoryNode, dependencyNode))
                        {
                            log.LogWarning("{0} has a package reference to a package produced in the same repo. {1} -> {2}", project.Repository.Name, Path.GetFileName(project.Path), packageDependency);
                        }
                        else
                        {
                            thisProjectRepositoryNode.Incoming.Add(dependencyNode);
                        }

                        dependencyNode.Outgoing.Add(thisProjectRepositoryNode);
                    }
                }
            }

            return graphNodes.Values.ToList();
        }
    }
}
