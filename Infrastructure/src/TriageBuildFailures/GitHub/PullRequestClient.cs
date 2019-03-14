using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.Commands;
using TriageBuildFailures.VSTS;
using TriageBuildFailures.VSTS.Models;

namespace TriageBuildFailures.GitHub
{
    public class PullRequestClient : ICIClient
    {
        private readonly GitHubClientWrapper _gitHubClient;
        private readonly VSTSClient _vstsClient;

        public PullRequestClient(GitHubClientWrapper githubClient, VSTSClient vstsClient)
        {
            _gitHubClient = githubClient;
            _vstsClient = vstsClient;
        }

        public Task<string> GetBuildLogAsync(ICIBuild build)
        {
            return _vstsClient.GetBuildLogAsync(build);
        }

        private const string TriageRequestStart = "@aspnet-hello triage ";

        public async Task<IEnumerable<ICIBuild>> GetFailedBuildsAsync(DateTime startDate)
        {
            var prs = await _gitHubClient.GetPullRequests("aspnet", "AspNetCore");
            prs = prs.Where(pr => pr.UpdatedAt >= startDate);
            var builds = new List<VSTSBuild>();
            foreach (var pr in prs)
            {
                var comments = await _gitHubClient.GetIssueComments(pr);
                var triageRequests = comments.Where(comment => comment.CreatedAt >= startDate && comment.Body.StartsWith(TriageRequestStart));
                foreach (var request in triageRequests)
                {
                    var url = request.Body.Replace(TriageRequestStart, "").Trim();
                    var build = await _vstsClient.GetBuild(url);
                    build.CIType = typeof(PullRequestClient);
                    build.PRSource = pr;
                    builds.Add(build);
                }
            }

            return builds;
        }

        public Task<IEnumerable<string>> GetTagsAsync(ICIBuild build)
        {
            return _vstsClient.GetTagsAsync(build);
        }

        public Task<string> GetTestFailureTextAsync(ICITestOccurrence failure)
        {
            return _vstsClient.GetTestFailureTextAsync(failure);
        }

        public Task<IEnumerable<ICITestOccurrence>> GetTestsAsync(ICIBuild build, BuildStatus? buildStatus = null)
        {
            return _vstsClient.GetTestsAsync(build);
        }

        public async Task SetTagAsync(ICIBuild build, string tag)
        {
            var vstsBuild = (VSTSBuild)build;
            if (vstsBuild.PRSource != null && string.Equals(tag, Triage.TriagedTag))
            {
                var comment = "I've triaged the above build.";
                await _gitHubClient.CreateComment(vstsBuild.PRSource, comment);
            }
            await _vstsClient.SetTagAsync(build, tag);
        }
    }
}
