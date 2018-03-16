// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.DotNet.VersionTools.Automation;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Dotnet.Scripts
{
    public static class Program
    {
        private static readonly Config _config = new Config();

        public static async Task Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            ParseArgs(args);

            await CreatePullRequest();
        }

        private static void ParseArgs(string[] args)
        {
            var builder = new ConfigurationBuilder().AddCommandLine(args);
            var configRoot = builder.Build();
            configRoot.Bind(_config);
        }

        private static async Task CreatePullRequest()
        {
            var gitHubAuth = new GitHubAuth(_config.GithubToken, _config.GithubUsername, _config.GithubEmail);
            var origin = new GitHubProject(_config.GithubProject, _config.GithubUsername);
            var upstreamBranch = new GitHubBranch(_config.GithubUpstreamBranch, new GitHubProject(_config.GithubProject, _config.GithubUpstreamOwner));

            var commitMessage = $"Updating external dependencies to '{ await GetOrchestratedBuildId() }'";
            var body = string.Empty;
            if (_config.GitHubPullRequestNotifications.Any())
            {
                body += PullRequestCreator.NotificationString(_config.GitHubPullRequestNotifications);
            }

            body += $"New versions:{Environment.NewLine}";

            foreach (var updatedVersion in _config.UpdatedVersionsList)
            {
                body += $"    {updatedVersion}{Environment.NewLine}";
            }

            await new PullRequestCreator(gitHubAuth, origin, upstreamBranch)
                .CreateOrUpdateAsync(commitMessage, commitMessage + $" ({upstreamBranch.Name})", body);
        }

        private static async Task<string> GetOrchestratedBuildId()
        {
            var xmlUrl = _config.BuildXml;

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(xmlUrl);
                using (var bodyStream = await response.Content.ReadAsStreamAsync())
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(bodyStream);
                    var orcBuilds = xmlDoc.GetElementsByTagName("OrchestratedBuild");

                    if (orcBuilds.Count < 1)
                    {
                        throw new ArgumentException($"{xmlUrl} didn't have an 'OrchestratedBuild' element.");
                    }

                    var orcBuild = orcBuilds[0];

                    return orcBuild.Attributes["BuildId"].Value;
                }
            }
        }
    }
}
