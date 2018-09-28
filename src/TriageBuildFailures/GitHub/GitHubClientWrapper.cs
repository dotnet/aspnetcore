// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using Octokit;

namespace TriageBuildFailures.GitHub
{
    public class GitHubClientWrapper
    {
        public const string TestFailureTag = "test-failure";
        public GitHubConfig Config { get; private set; }
        public GitHubClient Client { get; private set; }

        private readonly IReporter _reporter;
        private static readonly ProductHeaderValue ProductHeader = new ProductHeaderValue("rybrandeRAAS");

        public GitHubClientWrapper(GitHubConfig config, IReporter reporter)
        {
            var apiConnection = new ApiConnection(new Connection(ProductHeader));
            _reporter = reporter;
            Config = config;
            Client = new GitHubClient(ProductHeader)
            {
                Credentials = new Credentials(Config.AccessToken)
            };
        }

        /// <summary>
        /// Get the issues for a repo
        /// </summary>
        /// <param name="repo">The repo to retrieve issues for.</param>
        /// <returns>The issues which apply to the given repo.</returns>
        /// <remarks>We take care of repos which keep their issues on the home repo within this function.</remarks>
        public async Task<IEnumerable<GitHubIssue>> GetIssues(string owner, string repo)
        {
            var request = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open
            };

            if (IssuesOnHomeRepo(repo))
            {
                request.Labels.Add($"repo:{repo}");
                repo = "Home";
            }

            var issues = await Client.Issue.GetAllForRepository(owner, repo, request);
            return issues.Select(i => new GitHubIssue(i));
        }

        /// <summary>
        /// Get all the issues for the given repo which regard flaky issues.
        /// </summary>
        /// <param name="repo">The repo to search.</param>
        /// <returns>The list of flaky issues.</returns>
        public async Task<IEnumerable<GitHubIssue>> GetFlakyIssues(string owner, string repo)
        {
            var issues = await GetIssues(owner, repo);

            return issues.Where(i =>
                i.Title.StartsWith("Flaky", StringComparison.OrdinalIgnoreCase)
                || i.Title.StartsWith("Test failure:", StringComparison.OrdinalIgnoreCase)
                || i.Labels.Any(l =>
                    l.Name.Contains("Flaky", StringComparison.OrdinalIgnoreCase)
                    || l.Name.Contains(TestFailureTag, StringComparison.OrdinalIgnoreCase)));
        }

        private bool IssuesOnHomeRepo(string repoName)
        {
            var repo = Config.Repos.FirstOrDefault(r => r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));

            return repo == null ? false : repo.IssuesOnHomeRepo;
        }

        public async Task AddIssueToProject(GitHubIssue issue, int columnId)
        {
            var newCard = new NewProjectCard($"{issue.RepositoryOwner}/{issue.RepositoryName}#{issue.Number}");
            await Client.Repository.Project.Card.Create(columnId, newCard);
        }

        public async Task AddLabel(GitHubIssue issue, string label)
        {
            await Client.Issue.Labels.AddToIssue(issue.RepositoryOwner, issue.RepositoryName, issue.Number, new string[] { label });
        }

        public async Task<IEnumerable<IssueComment>> GetIssueComments(GitHubIssue issue)
        {
            return await Client.Issue.Comment.GetAllForIssue(issue.RepositoryOwner, issue.RepositoryName, issue.Number);
        }

        public async Task CreateComment(GitHubIssue issue, string comment)
        {
            comment += $"\n\nThis comment was made automatically. If there is a problem contact {Config.BuildBuddyUsername}.";

            await Client.Issue.Comment.Create(issue.RepositoryOwner, issue.RepositoryName, issue.Number, comment);
        }

        public async Task EditComment(GitHubIssue issue, IssueComment comment, string newBody)
        {
            await Client.Issue.Comment.Update(issue.RepositoryOwner, issue.RepositoryName, comment.Id, newBody);
        }

        public const int MaxBodyLength = 64000;

        public async Task<GitHubIssue> CreateIssue(string owner, string repo, string subject, string body, IList<string> labels)
        {
            if (IssuesOnHomeRepo(repo))
            {
                if (labels == null)
                {
                    labels = new List<string>();
                }

                labels.Add($"repo:{repo}");
                repo = "Home";
            }

            body = $"This issue was made automatically. If there is a problem contact {Config.BuildBuddyUsername}.\n\n{body}";

            if (body.Length > MaxBodyLength)
            {
                throw new ArgumentOutOfRangeException($"Body must be less than or equal to {MaxBodyLength} characters long.");
            }

            var newIssue = new NewIssue(subject)
            {
                Body = body,
            };

            foreach (var label in labels)
            {
                newIssue.Labels.Add(label);
            }

            return new GitHubIssue(await Client.Issue.Create(owner, repo, newIssue));
        }
    }
}
