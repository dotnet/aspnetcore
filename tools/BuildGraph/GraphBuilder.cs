using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildGraph
{
    public static class GraphBuilder
    {
        public static IList<GraphNode> Generate(IList<Repository> repositories)
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

                        thisProjectRepositoryNode.Incoming.Add(dependencyNode);
                        dependencyNode.Outgoing.Add(thisProjectRepositoryNode);
                    }
                }
            }

            return graphNodes.Values.ToList();
        }
    }
}