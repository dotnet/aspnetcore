using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.VSTS;

namespace TriageBuildFailures.GitHub
{
    public class PullRequestClient : ICIClient
    {
        private GitHubClientWrapper _gitHubClient;
        private VSTSClient _vstsClient;

        public PullRequestClient(GitHubClientWrapper githubClient, VSTSClient vstsClient)
        {
            _gitHubClient = githubClient;
            _vstsClient = vstsClient;
        }

        public Task<string> GetBuildLog(ICIBuild build)
        {
            return _vstsClient.GetBuildLog(build);
        }

        private const string TriageRequestStart = "@aspnet-hello triage ";

        public async Task<IEnumerable<ICIBuild>> GetFailedBuilds(DateTime startDate)
        {
            var prs = await _gitHubClient.GetPullRequests("aspnet", "AspNetCore");
            var builds = new List<ICIBuild>();
            foreach (var pr in prs)
            {
                var comments = await _gitHubClient.GetIssueComments(pr);
                var triageRequests = comments.Where(comment => comment.CreatedAt >= startDate && comment.Body.StartsWith(TriageRequestStart));
                foreach (var request in triageRequests)
                {
                    var url = request.Body.Replace(TriageRequestStart, "").Trim();
                    builds.Add(await _vstsClient.GetBuild(url));
                }
            }

            return builds;
        }

        public Task<IEnumerable<string>> GetTags(ICIBuild build)
        {
            return _vstsClient.GetTags(build);
        }

        public Task<string> GetTestFailureText(ICITestOccurrence failure)
        {
            return _vstsClient.GetTestFailureText(failure);
        }

        public Task<IEnumerable<ICITestOccurrence>> GetTests(ICIBuild build, BuildStatus? buildStatus = null)
        {
            return _vstsClient.GetTests(build);
        }

        public Task SetTag(ICIBuild build, string tag)
        {
            return _vstsClient.SetTag(build, tag);
        }
    }
}
