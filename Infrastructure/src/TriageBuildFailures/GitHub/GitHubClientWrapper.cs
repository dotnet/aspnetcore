// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Octokit;

namespace TriageBuildFailures.GitHub
{
    public class GitHubClientWrapper
    {
        public const string TestFailureTag = "test-failure";
        public GitHubConfig Config { get; private set; }
        public GitHubClient Client { get; private set; }

        private readonly IReporter _reporter;
        private static readonly Random _random = new Random();
        private const string _tempFolder = "temp";

        public const int MaxBodyLength = 64000;

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
            var key = GetIssueCacheKey(owner, repo);
            if (!MemoryCache.Default.Contains(key))
            {
                var request = new RepositoryIssueRequest
                {
                    State = ItemStateFilter.Open
                };

                var issues = await RetryHelpers.RetryAsync(async () => await Client.Issue.GetAllForRepository(owner, repo, request), _reporter);
                var results = issues.Select(i => new GitHubIssue(i));

                var policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(2))
                };

                MemoryCache.Default.Set(new CacheItem(key, results), policy);
            }


            return MemoryCache.Default[key] as IEnumerable<GitHubIssue>;
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

        public async Task AddIssueToProject(GitHubIssue issue, int columnId)
        {
            var newCard = new NewProjectCard($"{issue.RepositoryOwner}/{issue.RepositoryName}#{issue.Number}");
            await RetryHelpers.RetryAsync(async () => await Client.Repository.Project.Card.Create(columnId, newCard), _reporter);
        }

        public async Task AddLabel(GitHubIssue issue, string label)
        {
            await RetryHelpers.RetryAsync(async () => await Client.Issue.Labels.AddToIssue(issue.RepositoryOwner, issue.RepositoryName, issue.Number, new string[] { label }), _reporter);
        }

        public async Task<IEnumerable<IssueComment>> GetIssueComments(GitHubIssue issue)
        {
            return await RetryHelpers.RetryAsync(async () => await Client.Issue.Comment.GetAllForIssue(issue.RepositoryOwner, issue.RepositoryName, issue.Number), _reporter);
        }

        public async Task CreateComment(GitHubIssue issue, string comment)
        {
            comment = $"This comment was made automatically. If there is a problem contact {Config.BuildBuddyUsername}.\n\n{comment}";

            await RetryHelpers.RetryAsync(async () => await Client.Issue.Comment.Create(issue.RepositoryOwner, issue.RepositoryName, issue.Number, comment), _reporter);
        }

        public async Task EditComment(GitHubIssue issue, IssueComment comment, string newBody)
        {
            await RetryHelpers.RetryAsync(async () => await Client.Issue.Comment.Update(issue.RepositoryOwner, issue.RepositoryName, comment.Id, newBody), _reporter);
        }

        public async Task<GitHubIssue> CreateIssue(string owner, string repo, string subject, string body, IList<string> labels, IEnumerable<string> assignees, IList<KeyValuePair<string, object>> hiddenData)
        {
            var hiddenDataStringBuilder = new StringBuilder();

            if (hiddenData != null)
            {
                hiddenDataStringBuilder.AppendLine("<details>");
                hiddenDataStringBuilder.AppendLine("<summary>Additional details</summary>");
                hiddenDataStringBuilder.AppendLine("<table>");

                hiddenDataStringBuilder.AppendLine("<thead>");
                hiddenDataStringBuilder.AppendLine("<tr>");
                hiddenDataStringBuilder.AppendLine("<th>");
                hiddenDataStringBuilder.AppendLine("Key");
                hiddenDataStringBuilder.AppendLine("</th>");
                hiddenDataStringBuilder.AppendLine("<th>");
                hiddenDataStringBuilder.AppendLine("Value");
                hiddenDataStringBuilder.AppendLine("</th>");
                hiddenDataStringBuilder.AppendLine("</tr>");
                hiddenDataStringBuilder.AppendLine("</thead>");

                hiddenDataStringBuilder.AppendLine("<tbody>");
                foreach (var hiddenDataRow in hiddenData)
                {
                    hiddenDataStringBuilder.AppendLine("<tr>");

                    hiddenDataStringBuilder.AppendLine("<td>");
                    hiddenDataStringBuilder.AppendLine(HtmlEncoder.Default.Encode(hiddenDataRow.Key));
                    hiddenDataStringBuilder.AppendLine("</td>");

                    hiddenDataStringBuilder.AppendLine("<td>");
                    hiddenDataStringBuilder.AppendLine("<pre>");
                    hiddenDataStringBuilder.AppendLine(HtmlEncoder.Default.Encode(JsonConvert.SerializeObject(hiddenDataRow.Value, Formatting.Indented)));
                    hiddenDataStringBuilder.AppendLine("</pre>");
                    hiddenDataStringBuilder.AppendLine("</td>");

                    hiddenDataStringBuilder.AppendLine("</tr>");
                }
                hiddenDataStringBuilder.AppendLine("</tbody>");
                hiddenDataStringBuilder.AppendLine("</table>");
                hiddenDataStringBuilder.AppendLine("</details>");
            }

            var fullBody = $@"This issue was made automatically. If there is a problem contact {Config.BuildBuddyUsername}.

{body}

{hiddenDataStringBuilder.ToString()}";

            if (fullBody.Length > MaxBodyLength)
            {
                throw new ArgumentOutOfRangeException($"Body must be less than or equal to {MaxBodyLength} characters long.");
            }

            var newIssue = new NewIssue(subject)
            {
                Body = fullBody,
            };

            if (assignees != null)
            {
                foreach (var assignee in assignees)
                {
                    newIssue.Assignees.Add(assignee);
                }
            }

            if (labels != null)
            {
                foreach (var label in labels)
                {
                    newIssue.Labels.Add(label);
                }
            }

            // Before applying labels, make sure the labels exist
            await EnsureLabelsExist(owner, repo, labels);

            MemoryCache.Default.Remove(GetIssueCacheKey(owner, repo));
            return new GitHubIssue(await RetryHelpers.RetryAsync(async () => await Client.Issue.Create(owner, repo, newIssue), _reporter));
        }

        private async Task EnsureLabelsExist(string owner, string repo, IList<string> labels)
        {
            // TODO: Do caching stuff?

            var allLabelsInRepo = await RetryHelpers.RetryAsync(async () => await Client.Issue.Labels.GetAllForRepository(owner, repo), _reporter);
            var labelsThatDontExist = labels.Except(allLabelsInRepo.Select(label => label.Name), StringComparer.OrdinalIgnoreCase).ToList();
            if (labelsThatDontExist.Any())
            {
                foreach (var labelThatDoesntExist in labelsThatDontExist)
                {
                    var newLabel = new NewLabel(labelThatDoesntExist, "e89f02"); // ugly orange
                    await RetryHelpers.RetryAsync(async () => await Client.Issue.Labels.Create(owner, repo, newLabel), _reporter);
                }
            }
        }

        private string GetIssueCacheKey(string owner, string repo)
        {
            return $"{owner}/{repo}";
        }
    }
}
