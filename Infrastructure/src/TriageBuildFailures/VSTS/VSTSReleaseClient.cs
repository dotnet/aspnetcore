// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.VSTS.Models;

namespace TriageBuildFailures.VSTS
{
    public class VSTSReleaseClient : VSTSClientBase, ICIClient
    {
        private readonly GitHubClientWrapper _gitHubClient;

        public VSTSReleaseClient(VSTSConfig vstsConfig, GitHubClientWrapper gitHubClient, IReporter reporter)
            : base(vstsConfig, reporter)
        {
            _gitHubClient = gitHubClient;
        }

        public async Task<string> GetBuildLogAsync(ICIBuild build)
        {
            var release = (VSTSRelease)build;
            var sb = new StringBuilder();

            // Gotta love a nested object
            foreach (var env in release.Environments)
            {
                foreach (var step in env.DeploySteps)
                {
                    foreach (var phase in step.ReleaseDeployPhases)
                    {
                        foreach (var deploymentJob in phase.DeploymentJobs)
                        {
                            sb.Append(await GetTaskLogAsync(deploymentJob.Job));
                            foreach (var task in deploymentJob.Tasks)
                            {
                                sb.Append(await GetTaskLogAsync(task));
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        public async Task<IEnumerable<ICIBuild>> GetFailedBuildsAsync(DateTime startDate)
        {
            var projects = await GetProjects();

            var statuses = new EnvironmentStatus[] {
                EnvironmentStatus.Canceled,
                EnvironmentStatus.PartiallySucceeded,
                EnvironmentStatus.Rejected
            };
            var results = new List<VSTSRelease>();
            foreach (var project in projects)
            {
                var releases = await GetReleasesForProjectAsync(project, statuses, startDate);
                releases = releases.Where(r => r.ReleaseDefinition.Path.StartsWith("\\aspnet"));
                results.AddRange(releases.Select(r => new VSTSRelease(r)));
            }

            return await FilterTriagedReleasesAsync(results);
        }

        public async Task<IEnumerable<string>> GetTagsAsync(ICIBuild build)
        {
            var vstsBuild = (VSTSRelease)build;
            var result = await MakeVSTSRequest<VSTSArray<string>>(
                HttpMethod.Get,
                $"{vstsBuild.Project}/_apis/release/releases/{vstsBuild.Id}/tags",
                apiVersion: ApiVersion.V5_0_Preview,
                apiType: ApiType.VSRM);

            return result.Value;
        }

        public Task<string> GetTestFailureTextAsync(ICITestOccurrence failure)
        {
            // No tests in releases, so we should never hit this.
            throw new NotSupportedException();
        }

        public Task<IEnumerable<ICITestOccurrence>> GetTestsAsync(ICIBuild build, BuildStatus? buildStatus = null)
        {
            // Results don't have tests yet.
            IEnumerable<ICITestOccurrence> result = new List<ICITestOccurrence>();
            return Task.FromResult(result);
        }

        public Task SetTagAsync(ICIBuild build, string tag)
        {
            // We can't actually set a tag on a release at the moment :(
            return Task.CompletedTask;
        }

        private async Task<IEnumerable<VSTSRelease>> FilterTriagedReleasesAsync(IEnumerable<VSTSRelease> releases)
        {
            var results = new List<VSTSRelease>();

            foreach (var release in releases)
            {
                var issues = await _gitHubClient.GetIssues("aspnet", GitHubUtils.PrivateRepo);
                var applicable = issues.Where(i => string.Equals(i.Title, $"{release.BuildName} failed"));
                if (applicable.Count() > 0)
                {
                    var issue = applicable.First();
                    if (issue.Body.Contains(release.WebURL.AbsoluteUri))
                    {
                        continue;
                    }
                    var comments = await _gitHubClient.GetIssueComments(issue);
                    if (comments.Any(c => c.Body.Contains(release.WebURL.AbsoluteUri)))
                    {
                        continue;
                    }
                }

                results.Add(release);
            }

            return results;
        }

        private Task<string> GetTaskLogAsync(DeploymentJob job)
        {
            return GetTaskLogAsync(job.LogUri);
        }

        private Task<string> GetTaskLogAsync(DeploymentTask task)
        {
            return GetTaskLogAsync(task.LogUri);
        }

        private async Task<string> GetTaskLogAsync(Uri uri)
        {
            if (uri != null)
            {
                using (var stream = await HitVSTSUrlAsync(HttpMethod.Get, uri, "text/plain"))
                using (var sr = new StreamReader(stream))
                {
                    return await sr.ReadToEndAsync();
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private async Task<IEnumerable<Release>> GetReleasesForProjectAsync(VSTSProject project, IEnumerable<EnvironmentStatus> statuses, DateTime startDate)
        {
            var queryItems = new Dictionary<string, string>
            {
                ["minCreatedTime"] = ToVSTSString(startDate)
            };

            var thinReleases = (await MakeVSTSRequest<VSTSArray<ThinRelease>>(
                HttpMethod.Get,
                $"{project.Id}/_apis/release/releases",
                queryItems,
                ApiVersion.V5_0_Preview2,
                ApiType.VSRM)).Value;

            var releases = new List<Release>();
            foreach (var thin in thinReleases)
            {
                if (!Config.ReleaseIdIgnoreList.Any(r =>
                     string.Equals(r.Id, thin.ReleaseDefinition.Id, StringComparison.OrdinalCulture)
                     && string.Equals(r.Project, project.Name, StringComparison.OrdinalCulture)))
                {
                    releases.Add(await GetReleaseAsync(thin));
                }
            }

            return releases.Where(r => r.Environments.Any(e => statuses.Contains(e.Status)));
        }

        private Task<Release> GetReleaseAsync(ThinRelease thinRelease)
        {
            return MakeVSTSRequest<Release>(
                HttpMethod.Get,
                $"{thinRelease.ProjectReference.Id}/_apis/release/releases/{thinRelease.Id}",
                apiType: ApiType.VSRM);
        }
    }
}
