using McMaster.Extensions.CommandLineUtils;
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
        private readonly VSTSBuildClient _vstsClient;
        private readonly IReporter _reporter;

        public PullRequestClient(GitHubClientWrapper githubClient, VSTSBuildClient vstsClient, IReporter reporter)
        {
            _gitHubClient = githubClient;
            _vstsClient = vstsClient;
            _reporter = reporter;
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
                _reporter.Output($"     {pr.Url} is recent!");
                var comments = await _gitHubClient.GetIssueComments(pr);
                var triageRequests = comments.Where(comment => comment.CreatedAt >= startDate && comment.Body.StartsWith(TriageRequestStart));
                foreach (var request in triageRequests)
                {
                    _reporter.Output($"     {pr.Url} wants triage!");
                    var url = request.Body.Split(Environment.NewLine)[0].Replace(TriageRequestStart, "").Trim().Trim('.');
                    var build = await _vstsClient.GetBuild(url);

                    // Probably the url format was bad. Ignore it.
                    if (build == null)
                    {
                        _reporter.Output($"     {url} is an invalid format.");
                        continue;
                    }

                    build.Branch = "PR";
                    build.CIType = typeof(PullRequestClient);
                    build.PRSource = pr;

                    if (!build.Deleted)
                    {
                        builds.Add(build);
                    }
                    else
                    {
                        _reporter.Output($"     {url} has been deleted, ignoring it.");
                    }
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
            return _vstsClient.GetTestsAsync(build, buildStatus);
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
