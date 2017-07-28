using System.IO;
using NuGet.ProjectModel;

namespace RepoTools.BuildGraph
{
    public class DependencyGraphSpecProvider
    {
        readonly string _packageSpecDirectory;

        public DependencyGraphSpecProvider(string packageSpecDirectory)
        {
            _packageSpecDirectory = packageSpecDirectory;
        }

        public DependencyGraphSpec GetDependencyGraphSpec(string repositoryName, string solutionPath)
        {
            var outputFile = Path.Combine(_packageSpecDirectory, repositoryName, Path.GetFileName(solutionPath) + ".json");

            if (!File.Exists(outputFile))
            {
                return null;
            }

            return DependencyGraphSpec.Load(outputFile);
        }
    }
}
