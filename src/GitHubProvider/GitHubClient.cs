// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using Microsoft.Extensions.Tools.Internal;
using Common;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace GitHubProvider
{
    public class GitHubClient
    {
        private readonly string _gitHubAccessToken;
        private const string _owner = "aspnet";
        private IReporter _reporter;

        public GitHubClient(string gitHubAccessToken, IReporter reporter)
        {
            _reporter = reporter;
            _gitHubAccessToken = gitHubAccessToken;
        }

        private IDictionary<string, IEnumerable<GithubIssue>> _issueCache = new ConcurrentDictionary<string, IEnumerable<GithubIssue>>();

        public async Task<IEnumerable<GithubIssue>> GetIssues(string repo)
        {
            if (IssuesOnHomeRepo(repo))
            {
                repo = "Home";
            }

            if(!_issueCache.ContainsKey(repo))
            {
                _issueCache[repo] = await MakePagedGithubRequest<GithubIssue>(HttpMethod.Get, $"repos/{_owner}/{repo}/issues?per_page=100&q=is%3Aissue+is%3Aclosed");
            }

            return _issueCache[repo];
        }

        public async Task<IEnumerable<GithubIssue>> GetFlakyIssues(string repo)
        {
            var issues = await GetIssues(repo);

            return issues.Where(i => 
            i.Title.StartsWith("Flaky", StringComparison.InvariantCultureIgnoreCase)
            || i.Title.StartsWith("flakey", StringComparison.InvariantCultureIgnoreCase) 
            || i.Title.Contains(" fails")
            || i.Title.StartsWith("Broken"));
        }

        private IEnumerable<string> _issuesOnHomeRepo = new List<string> {
            "WebSockets"
        };

        private bool IssuesOnHomeRepo(string repo)
        {
            return _issuesOnHomeRepo.Contains(repo);
        }

        public async Task<GitHubProject> GetProjects()
        {
            return await MakeGithubRequest<GitHubProject>(HttpMethod.Get, $"org/projects");
        }

        public async Task AddIssueToProject(GithubIssue issue, GitHubProjectColumn column)
        {
            if(Static.BeQuite)
            {
                Directory.CreateDirectory("Project");

                using (var fileStream = File.CreateText(Path.Combine("Project", $"{ issue.Number}.txt")))
                {
                    fileStream.Write(issue.Id);
                }
            }
            else
            {
                var body = JsonConvert.SerializeObject(new Dictionary<string, string> {
                    { "note", $"aspnet/{issue.RepositoryName}#{issue.Number}" }
                });

                await MakeGithubRequest(HttpMethod.Post, $"org/projects/columns/{column.Id}/cards", body);
            }
        }

        public async Task<IEnumerable<GithubComment>> GetComments(GithubIssue issue)
        {
            return await MakePagedGithubRequest<GithubComment>(HttpMethod.Get, $"repos/aspnet/{issue.RepositoryName}/issues/{issue.Number}/comments");
        }

        public async Task CreateComment(GithubIssue issue, string comment)
        {
            if (Static.BeQuite)
            {
                var tempComments = Path.Combine("Comments", issue.Id.ToString());
                Directory.CreateDirectory(tempComments);

                using (var fileStream = File.CreateText(Path.Combine(tempComments, $"{Guid.NewGuid().ToString()}.txt")))
                {
                    fileStream.Write(comment);
                }
            }
            else
            {
                var body = JsonConvert.SerializeObject(new Dictionary<string, string> { { "body", comment } });
                await MakeGithubRequest(HttpMethod.Post, $"repos/aspnet/{issue.RepositoryName}/issues/{issue.Number}/comments", body);
            }
        }

        private static Random _random = new Random();

        public Task<GithubIssue> CreateIssue(string repo, string subject, string body, IEnumerable<string> labels)
        {
            if (Static.BeQuite)
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

                using (var fileStream = File.CreateText(Path.Combine(repo, $"{Guid.NewGuid().ToString()}.txt")))
                {
                    fileStream.Write(tempMsg);
                }

                return Task.FromResult(new GithubIssue {
                    Body = body,
                    Id = _random.Next(),
                    Title = subject,
                    Labels = labels.Select(s => new GitHubLabel { Name = s })
                });
            }
            else
            {
                var bodyStr = JsonConvert.SerializeObject(new Dictionary<string, object> {
                    { "title", subject},
                    { "body", body },
                    { "lables", labels}
                });

                // Invalidate the cache
                _issueCache.Remove(repo);

                return MakeGithubRequest<GithubIssue>(HttpMethod.Post, $"/repos/aspnet/{repo}/issues", bodyStr);
            }
        }

        private async Task<IEnumerable<T>> MakePagedGithubRequest<T>(HttpMethod method, string requestUri)
        {
            var responses = await MakePagedGithubRequest(method, requestUri);

            var objs = new List<T>();
            foreach (var response in responses)
            {
                objs.AddRange(await ResponseToObject<IEnumerable<T>>(response));
            }

            return objs;
        }

        private async Task<T> ResponseToObject<T>(HttpResponseMessage response)
        {
            var contentStream = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(contentStream);
        }

        private async Task<T> MakeGithubRequest<T>(HttpMethod method, string requestUri, string body = null)
        {
            var request = await MakeGithubRequest(method, requestUri, body);

            return await ResponseToObject<T>(request);
        }

        private async Task<HttpResponseMessage> MakeGithubRequest(HttpMethod method, string requestUri, string body = null)
        {
            using (var client = new HttpClient { BaseAddress = new Uri("https://api.github.com/") })
            {
                var request = new HttpRequestMessage(method, requestUri);

                request.Headers.Authorization = new AuthenticationHeaderValue("Token", _gitHubAccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.inertia-preview+json"));
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("rybrandeRAAS")));

                if(body != null)
                {
                    request.Content = new StringContent(body);
                }

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    throw new HttpRequestException($"Failed status code {response.StatusCode} on {request.RequestUri}. Content: {content}");
                }

                return response;
            }
        }

        private async Task<IEnumerable<HttpResponseMessage>> MakePagedGithubRequest(HttpMethod method, string requestUri)
        {
            var responses = new List<HttpResponseMessage>();

            bool morePages;
            do {
                morePages = false;
                var response = await MakeGithubRequest(method, requestUri);

                if (response.Headers.Contains("Link"))
                {
                    var links = response.Headers.GetValues("Link").First().Split(',');
                    var next = links.SingleOrDefault(l => l.EndsWith("rel=\"next\""));

                    if (next != null)
                    {
                        morePages = true;
                        requestUri = next.Split(';')[0].Trim('<', '>', ' ');
                    }
                }

                responses.Add(response);
            } while (morePages);

            return responses;
        }
    }
}
