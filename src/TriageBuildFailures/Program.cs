// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.Handlers;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await new Triage().TriageFailures();

            Console.ReadKey();
        }
    }

    public class Triage
    {
        public static string TriagedTag = "Triaged";

        private readonly TeamCityClientWrapper _tcClient;
        private readonly GitHubClientWrapper _ghClient;
        private readonly EmailClient _emailClient;
        private IReporter _reporter;

        public Triage()
        {
            var config = GetConfig();
            _reporter = GetReporter();
            _tcClient = GetTeamCityClient(config);
            _ghClient = GetGitHubClient(config);
            _emailClient = GetEmailClient(config);
        }

        public static Config GetConfig()
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
        }

        private DateTime CutoffDate { get; } = DateTime.Now.AddHours(-12);

        /// <summary>
        /// Handle each CI failure in the most appropriate way.
        /// </summary>
        /// <returns>A task indicating completion.</returns>
        public async Task TriageFailures()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var builds = GetUnTriagedFailures();

            foreach(var build in builds)
            {
                await HandleFailure(build);
            }
            stopWatch.Stop();

            _reporter.Output($"There were {builds.Count()} untriaged failures since {CutoffDate} and we handled them in {stopWatch.Elapsed.TotalMinutes} minutes. Let's get some coffee!");
        }

        /// <summary>
        /// Take the appropriate action for a CI failure.
        /// </summary>
        /// <param name="build">The CI failure which we should handle.</param>
        /// <returns></returns>
        private async Task HandleFailure(TeamCityBuild build)
        {
            var handlers = new List<HandleFailureBase> { new HandleMSRCBuilds(), new HandleLowValueBuilds(),
                new HandleUniverseMovedOn(), new HandleTestFailures(),
                new HandleBuildTimeFailures(), new HandleUnhandled() };

            foreach(var handler in handlers)
            {
                handler.TCClient = _tcClient;
                handler.GHClient = _ghClient;
                handler.EmailClient = _emailClient;
                handler.Reporter = _reporter;
                
                if(handler.CanHandleFailure(build))
                {
                    await handler.HandleFailure(build);
                    MarkTriaged(build);
                    return;
                }
            }
        }

        /// <summary>
        /// Gets the list of CI failures which have not been previously triaged since the CutoffDate
        /// </summary>
        /// <returns>The list of CI failures which have not been previously triaged.</returns>
        private IEnumerable<TeamCityBuild> GetUnTriagedFailures()
        {
            var failedBuilds = _tcClient.GetFailedBuilds(CutoffDate);

            foreach (var failedBuild in failedBuilds)
            {
                var tags = _tcClient.GetTags(failedBuild);
                if (!tags.Contains(TriagedTag))
                {
                    yield return failedBuild;
                }
            }
        }

        private void MarkTriaged(TeamCityBuild build)
        {
            _tcClient.SetTag(build, TriagedTag);
        }
        
        private static IReporter GetReporter()
        {
            return new ConsoleReporter(PhysicalConsole.Singleton);
        }

        private GitHubClientWrapper GetGitHubClient(Config config)
        {
            return new GitHubClientWrapper(config.GitHub, _reporter);
        }

        private EmailClient GetEmailClient(Config config)
        {
            return new EmailClient(config.Email ,_reporter);
        }

        private TeamCityClientWrapper GetTeamCityClient(Config config)
        {
            return new TeamCityClientWrapper(config.TeamCity, _reporter);
        }
    }
}
