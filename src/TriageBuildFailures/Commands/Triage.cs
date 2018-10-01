// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.Handlers;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Commands
{
    public class Triage
    {
        public static readonly string TriagedTag = "Triaged";

        private readonly TeamCityClientWrapper _tcClient;
        private readonly GitHubClientWrapper _ghClient;
        private readonly EmailClient _emailClient;
        private IReporter _reporter;
        private readonly Config _config;

        public Triage(Config config, IReporter reporter)
        {
            _config = config;
            _reporter = reporter;
            _tcClient = GetTeamCityClient(_config);
            _ghClient = GetGitHubClient(_config);
            _emailClient = GetEmailClient(_config);
        }

        private DateTime CutoffDate { get; } = DateTime.Now.AddHours(-24);

        /// <summary>
        /// Handle each CI failure in the most appropriate way.
        /// </summary>
        /// <returns>A task indicating completion.</returns>
        public async Task TriageFailures()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var untriagedBuildFailures = GetUntriagedBuildFailures().ToList();
            _reporter.Output($"We found {untriagedBuildFailures.Count} failed builds since {CutoffDate}. Let's triage!");

            // Write out some stats for TeamCity
            // More info here: https://confluence.jetbrains.com/display/TCD10/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ReportingBuildStatistics
            _reporter.Output($"##teamcity[buildStatisticValue key='RAAS:UntriagedBuildFailures' value='{untriagedBuildFailures.Count}']");

            foreach (var build in untriagedBuildFailures)
            {
                _reporter.Output($"Triaging {build.WebURL} ...");
                await HandleFailure(build);
            }
            stopWatch.Stop();

            _reporter.Output($"Done! Finished in {stopWatch.Elapsed.TotalMinutes} minutes. Let's get some coffee!");
        }

        private static readonly IEnumerable<HandleFailureBase> Handlers = new List<HandleFailureBase>
        {
            new HandleLowValueBuilds(),
            new HandleNonAllowedBuilds(),
            new HandleUniverseMovedOn(),
            new HandleSnapshotDependency(),
            new HandleTestFailures(),
            new HandleBuildTimeFailures(),
            new HandleUnhandled(),
        };

        /// <summary>
        /// Take the appropriate action for a CI failure.
        /// </summary>
        /// <param name="build">The CI failure which we should handle.</param>
        /// <returns></returns>
        private async Task HandleFailure(TeamCityBuild build)
        {
            foreach (var handler in Handlers)
            {
                handler.TCClient = _tcClient;
                handler.GHClient = _ghClient;
                handler.EmailClient = _emailClient;
                handler.Reporter = _reporter;
                handler.Config = _config;

                if (handler.CanHandleFailure(build))
                {
                    _reporter.Output($"Handling failure with '{handler.GetType().FullName}' handler");
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
        private IEnumerable<TeamCityBuild> GetUntriagedBuildFailures()
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

        private GitHubClientWrapper GetGitHubClient(Config config)
        {
            return new GitHubClientWrapper(config.GitHub, _reporter);
        }

        private EmailClient GetEmailClient(Config config)
        {
            return new EmailClient(config.Email, _reporter);
        }

        private TeamCityClientWrapper GetTeamCityClient(Config config)
        {
            return new TeamCityClientWrapper(config.TeamCity, _reporter);
        }
    }
}
