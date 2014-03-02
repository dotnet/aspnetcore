using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;

namespace TCDependencyManager
{
    class Program
    {
        private static readonly string[] _excludedRepos = new[] { "xunit", "kruntime", "coreclr", "universe" };

        static int Main(string[] args)
        {
            var teamCityUrl = GetEnv("TEAMCITY_SERVERURL");
            var teamCityUser = GetEnv("TEAMCITY_USER");
            var teamCityPass = GetEnv("TEAMCITY_PASSWORD");
            var githubCreds = GetEnv("GITHUB_CREDS");

            var teamCity = new TeamCityAPI(teamCityUrl, 
                                           new NetworkCredential(teamCityUser, teamCityPass));

            var gitHub = new GitHubAPI(githubCreds);

            Console.WriteLine("Listing GitHub repos");
            var repos = gitHub.GetRepos()
                           .Where(repo => !_excludedRepos.Contains(repo.Name, StringComparer.OrdinalIgnoreCase))
                           .ToList();

            Console.WriteLine("Listing projects under repos");
            var projects = repos.AsParallel()
                                .SelectMany(repo => gitHub.GetProjects(repo))
                                .ToList();


            Console.WriteLine("Creating dependency tree");
            MapRepoDependencies(projects);

            Console.WriteLine("Ensuring depndencies are consistent on TeamCity");
            foreach (var repo in repos.Where(p => p.Dependencies.Any()))
            {
                teamCity.EnsureDependencies(repo.Name, repo.Dependencies.Select(r => r.Name));
            }
            return 0;
        }

        private static void MapRepoDependencies(List<Project> projects)
        {
            var projectLookup = projects.ToDictionary(project => project.ProjectName, StringComparer.OrdinalIgnoreCase);

            foreach (var project in projects)
            {
                foreach (var dependency in project.Dependencies)
                {
                    Project dependencyProject;
                    if (projectLookup.TryGetValue(dependency, out dependencyProject) &&
                        project.Repo != dependencyProject.Repo)
                    {
                        project.Repo.Dependencies.Add(dependencyProject.Repo);
                    }
                }

            }
        }

        private static string GetEnv(string key)
        {
            var envValue = Environment.GetEnvironmentVariable(key);
            if (String.IsNullOrEmpty(envValue))
            {
                throw new ArgumentNullException(key);
            }
            return envValue;
        }

    }
}
