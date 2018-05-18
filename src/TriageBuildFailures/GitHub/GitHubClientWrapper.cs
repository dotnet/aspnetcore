// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Common;
using McMaster.Extensions.CommandLineUtils;
using Octokit;

namespace TriageBuildFailures.GitHub
{
    public class GitHubClientWrapper
    {
        public GitHubConfig Config { get; private set; }
        public GitHubClient Client { get; private set; }
        public ProjectColumnsClient ColumnsClient { get; private set; }
        public ProjectCardsClient CardClient { get; private set; }
        private IReporter _reporter;
        private static Random _random = new Random();
        private const string _tempFolder = "temp";

        private static readonly IEnumerable<string> _issuesOnHomeRepo = new List<string> {
            "Antiforgery",
            "Common",
            "CORS",
            "DataProtection",
            "DependencyInjection",
            "Diagnostics",
            "EventNotification",
            "FileSystem",
            "HttpAbstractions",
            "JsonPatch",
            "Localization",
            "Options",
            "Proxy",
            "ResponseCaching",
            "Routing",
            "Session",
            "StaticFiles",
            "Testing",
            "WebSockets"
        };
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
            ColumnsClient = new ProjectColumnsClient(apiConnection);
            CardClient = new ProjectCardsClient(apiConnection);
        }

        /// <summary>
        /// Get the issues for a repo
        /// </summary>
        /// <param name="repo">The repo to retrieve issues for.</param>
        /// <returns>The issues which apply to the given repo.</returns>
        /// <remarks>We take care of repos which keep their issues on the home repo within this function.</remarks>
        public async Task<IEnumerable<GithubIssue>> GetIssues(string owner, string repo)
        {
            string repoLabel = null;
            if (IssuesOnHomeRepo(repo))
            {
                repoLabel = $"repo:{repo}";
                repo = "Home";
            }

            var request = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open
            };
            if (repoLabel != null)
            {
                request.Labels.Add(repoLabel);
            }

            var issues = await Client.Issue.GetAllForRepository(owner, repo, request);
            return issues.Select(i => new GithubIssue(i));
        }

        /// <summary>
        /// Get all the issues for the given repo which regard flaky issues.
        /// </summary>
        /// <param name="repo">The repo to search.</param>
        /// <returns>The list of flaky issues.</returns>
        public async Task<IEnumerable<GithubIssue>> GetFlakyIssues(string owner, string repo)
        {
            var issues = await GetIssues(owner, repo);

            return issues.Where(i =>
            i.Title.StartsWith("Flaky", StringComparison.InvariantCultureIgnoreCase)
            || i.Title.StartsWith("flakey", StringComparison.InvariantCultureIgnoreCase)
            || i.Labels.Any(l => l.Name.Contains("Flaky", StringComparison.InvariantCultureIgnoreCase)));
        }

        private static bool IssuesOnHomeRepo(string repo)
        {
            return _issuesOnHomeRepo.Contains(repo);
        }

        public async Task AddIssueToProject(GithubIssue issue, int columnId)
        {
            if (Constants.BeQuite)
            {
                var folder = Path.Combine("temp", "Project");
                Directory.CreateDirectory(folder);

                using (var fileStream = File.CreateText(Path.Combine(folder, $"{issue.Number}.txt")))
                {
                    fileStream.Write(issue.Id);
                }
            }
            else
            {
                var newCard = new NewProjectCard($"{issue.RepositoryOwner}/{issue.RepositoryName}#{issue.Number}");
                await CardClient.Create(columnId, newCard);
            }
        }

        public async Task<IEnumerable<IssueComment>> GetIssueComments(GithubIssue issue)
        {
            return await Client.Issue.Comment.GetAllForIssue(issue.RepositoryOwner, issue.RepositoryName, issue.Number);
        }

        public async Task CreateComment(GithubIssue issue, string comment)
        {
            if (Constants.BeQuite)
            {
                var tempComments = Path.Combine(_tempFolder, issue.RepositoryOwner, issue.RepositoryName, issue.Number.ToString());
                Directory.CreateDirectory(tempComments);

                using (var fileStream = File.CreateText(Path.Combine(tempComments, $"{Path.GetRandomFileName()}.txt")))
                {
                    fileStream.Write(comment);
                }
            }
            else
            {
                await Client.Issue.Comment.Create(issue.RepositoryOwner, issue.RepositoryName, issue.Number, comment);
            }
        }

        public async Task<GithubIssue> CreateIssue(string owner, string repo, string subject, string body, IEnumerable<string> labels)
        {
            if (Constants.BeQuite)
            {
                var tempMsg = $@"Tried to create a github issue:
                    Repo: {repo}
                    Subject: {subject}
                    Body: {body}
                    Labels: {string.Join(",", labels)}";

                _reporter.Output(tempMsg);

                var repoFolder = Path.Combine(_tempFolder, owner, repo);
                Directory.CreateDirectory(repoFolder);

                using (var fileStream = File.CreateText(Path.Combine(repoFolder, $"{Path.GetRandomFileName()}.txt")))
                {
                    fileStream.Write(tempMsg);
                }

                var repository = await Client.Repository.Get(owner, repo);

                var issue = new Issue(url: null, htmlUrl: null, commentsUrl: null, eventsUrl: null, number: _random.Next(),
                    ItemState.Open, subject, body, null, null, null, null, null, null, 0, null, null, DateTimeOffset.Now,
                    DateTimeOffset.Now, _random.Next(), false, repository);
                return new GithubIssue(issue);
            }
            else
            {
                var newIssue = new NewIssue(subject)
                {
                    Body = body,
                };

                foreach (var label in labels)
                {
                    newIssue.Labels.Add(label);
                }

                return new GithubIssue(await Client.Issue.Create(owner, repo, newIssue));
            }
        }
    }

    public class GithubIssue : Issue
    {
        public GithubIssue(Issue issue)
            : base(issue.Url, issue.HtmlUrl, issue.CommentsUrl, issue.EventsUrl, issue.Number, issue.State.Value, issue.Title,
                  issue.Body, issue.ClosedBy, issue.User, issue.Labels, issue.Assignee, issue.Assignees, issue.Milestone,
                  issue.Comments, issue.PullRequest, issue.ClosedAt, issue.CreatedAt, issue.UpdatedAt, issue.Id, issue.Locked,
                  issue.Repository)
        {
        }

        public string RepositoryName
        {
            get
            {
                return Repository?.Name == null ? Url.Split('/')[5] : Repository.Name;
            }
        }

        public string RepositoryOwner
        {
            get
            {
                return Repository?.Owner == null ? Url.Split('/')[4] : Repository.Owner.Name;
            }
        }
    }
}
