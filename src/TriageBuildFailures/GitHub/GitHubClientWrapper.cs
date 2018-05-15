﻿// Copyright (c) .NET Foundation. All rights reserved.
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
        private const string _owner = "aspnet";
        public GitHubConfig Config { get; private set; }
        public GitHubClient Client { get; private set; }
        public ProjectColumnsClient ColumnsClient { get; private set; }
        public ProjectCardsClient CardClient { get; private set; }
        private IReporter _reporter;
        private static Random _random = new Random();

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
            Client = new GitHubClient(ProductHeader);
            Client.Credentials = new Credentials(Config.AccessToken);
            ColumnsClient = new ProjectColumnsClient(apiConnection);
            CardClient = new ProjectCardsClient(apiConnection);
        }

        /// <summary>
        /// Get the issues for a repo
        /// </summary>
        /// <param name="repo">The repo to retrieve issues for.</param>
        /// <returns>The issues which apply to the given repo.</returns>
        /// <remarks>We take care of repos which keep their issues on the home repo within this function.</remarks>
        public async Task<IEnumerable<Issue>> GetIssues(string repo)
        {
            string repoLabel = null;
            if (IssuesOnHomeRepo(repo))
            {
                repoLabel = $"repo:{repo}";
                repo = "Home";
            }

            var request = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open,
            };
            if (repoLabel != null)
            {
                request.Labels.Add(repoLabel);
            }

            return await Client.Issue.GetAllForRepository(_owner, repo, request);
        }

        /// <summary>
        /// Get all the issues for the given repo which regard flaky issues.
        /// </summary>
        /// <param name="repo">The repo to search.</param>
        /// <returns>The list of flaky issues.</returns>
        public async Task<IEnumerable<Issue>> GetFlakyIssues(string repo)
        {
            var issues = await GetIssues(repo);

            return issues.Where(i =>
            i.Title.StartsWith("Flaky", StringComparison.InvariantCultureIgnoreCase)
            || i.Title.StartsWith("flakey", StringComparison.InvariantCultureIgnoreCase)
            || i.Labels.Any(l => l.Name.Contains("Flaky")));
        }

        private static bool IssuesOnHomeRepo(string repo)
        {
            return _issuesOnHomeRepo.Contains(repo);
        }

        public async Task AddIssueToProject(Issue issue, int columnId)
        {
            if (Constants.BeQuite)
            {
                Directory.CreateDirectory("Project");

                using (var fileStream = File.CreateText(Path.Combine("Project", $"{ issue.Number}.txt")))
                {
                    fileStream.Write(issue.Id);
                }
            }
            else
            {
                var newCard = new NewProjectCard($"{_owner}/{issue.Repository}#{issue.Number}");
                await CardClient.Create(columnId, newCard);
            }
        }

        public async Task<IEnumerable<IssueComment>> GetIssueComments(Issue issue)
        {
            return await Client.Issue.Comment.GetAllForIssue(_owner, issue.Repository.Name, issue.Number);
        }

        public async Task CreateComment(Issue issue, string comment)
        {
            if (Constants.BeQuite)
            {
                var tempComments = Path.Combine("Comments", issue.Id.ToString());
                Directory.CreateDirectory(tempComments);

                using (var fileStream = File.CreateText(Path.Combine(tempComments, $"{Path.GetRandomFileName()}.txt")))
                {
                    fileStream.Write(comment);
                }
            }
            else
            {
                await Client.Issue.Comment.Create(_owner, issue.Repository.Name, issue.Number, comment);
            }
        }

        public async Task<Issue> CreateIssue(string repo, string subject, string body, IEnumerable<string> labels)
        {
            if (Constants.BeQuite)
            {
                var tempMsg = $@"Tried to create a github issue:
                    Repo: {repo}
                    Subject: {subject}
                    Body: {body}
                    Labels: {string.Join(",", labels)}";

                _reporter.Output(tempMsg);

                if (!Directory.Exists(repo))
                {
                    Directory.CreateDirectory(repo);
                }

                using (var fileStream = File.CreateText(Path.Combine(repo, $"{Path.GetRandomFileName()}.txt")))
                {
                    fileStream.Write(tempMsg);
                }

                return null;
            }
            else
            {
                var newIssue = new NewIssue(subject)
                {
                    Body = body,
                };
                
                foreach(var label in labels)
                {
                    newIssue.Labels.Add(label);
                }

                return await Client.Issue.Create(_owner, repo, newIssue);
            }
        }

        //private async Task<IEnumerable<T>> MakePagedGithubRequest<T>(HttpMethod method, string requestUri)
        //{
        //    var responses = await MakePagedGithubRequest(method, requestUri);

        //    var objs = new List<T>();
        //    foreach (var response in responses)
        //    {
        //        objs.AddRange(await ResponseToObject<IEnumerable<T>>(response));
        //    }

        //    return objs;
        //}

        //private async Task<T> ResponseToObject<T>(HttpResponseMessage response)
        //{
        //    var contentStream = await response.Content.ReadAsStringAsync();
        //    return JsonConvert.DeserializeObject<T>(contentStream);
        //}

        //private async Task<T> MakeGithubRequest<T>(HttpMethod method, string requestUri, string body = null)
        //{
        //    var request = await MakeGithubRequest(method, requestUri, body);

        //    return await ResponseToObject<T>(request);
        //}

        //private async Task<HttpResponseMessage> MakeGithubRequest(HttpMethod method, string requestUri, string body = null)
        //{
        //    using (var client = new HttpClient { BaseAddress = new Uri("https://api.github.com/") })
        //    {
        //        var request = new HttpRequestMessage(method, requestUri);

        //        request.Headers.Authorization = new AuthenticationHeaderValue("Token", Config.AccessToken);
        //        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.inertia-preview+json"));
        //        request.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("rybrandeRAAS")));

        //        if (body != null)
        //        {
        //            request.Content = new StringContent(body);
        //        }

        //        var response = await client.SendAsync(request);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();

        //            throw new HttpRequestException($"Failed status code {response.StatusCode} on {request.RequestUri}. Content: {content}");
        //        }

        //        return response;
        //    }
        //}

        //private async Task<IEnumerable<HttpResponseMessage>> MakePagedGithubRequest(HttpMethod method, string requestUri)
        //{
        //    var responses = new List<HttpResponseMessage>();

        //    bool morePages;
        //    do
        //    {
        //        morePages = false;
        //        var response = await MakeGithubRequest(method, requestUri);

        //        if (response.Headers.Contains("Link"))
        //        {
        //            var links = response.Headers.GetValues("Link").First().Split(',');
        //            var next = links.SingleOrDefault(l => l.EndsWith("rel=\"next\""));

        //            if (next != null)
        //            {
        //                morePages = true;
        //                requestUri = next.Split(';')[0].Trim('<', '>', ' ');
        //            }
        //        }

        //        responses.Add(response);
        //    } while (morePages);

        //    return responses;
        //}
    }
}
