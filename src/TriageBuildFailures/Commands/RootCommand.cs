// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.Handlers;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Commands
{
    internal class RootCommand : CommandBase
    {
        private CommandOption _gitHubAccessToken;
        private CommandOption _teamCityUserName;
        private CommandOption _teamCityPassword;
        private CommandOption _smtpLogin;
        private CommandOption _smtpPassword;

        private IReporter _reporter;

        protected override void ConfigureCore(CommandLineApplication application)
        {
            _gitHubAccessToken = application.Option("-ghat|--github-access-token <ACCESSTOKEN>", "", CommandOptionType.SingleValue);
            _teamCityUserName = application.Option("-tcun|--team-city-username <TCUSERNAME>", "", CommandOptionType.SingleValue);
            _teamCityPassword = application.Option("-tcpw|--team-city-password <TCPASSWORD>", "", CommandOptionType.SingleValue);
            _smtpLogin = application.Option("-sl|--smtp-login <SMTPLOGIN>", "", CommandOptionType.SingleValue);
            _smtpPassword = application.Option("-sp|--smtp-password <SMTPPASSWORD>", "", CommandOptionType.SingleValue);
            _reporter = GetReporter();
        }

        private static IReporter GetReporter()
        {
            return new ConsoleReporter(PhysicalConsole.Singleton);
        }

        protected override async Task<int> Execute()
        {
            try
            {
                await new Triage(GetConfig(), _reporter).TriageFailures();
            }
            catch(Exception ex)
            {
                _reporter.Error(ex.ToString());
                return 1;
            }
            return 0;
        }

        public Config GetConfig()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            config.GitHub.AccessToken = _gitHubAccessToken.Value();
            config.Email.SmtpConfig.Login = _smtpLogin.Value();
            config.Email.SmtpConfig.Password = _smtpPassword.Value();
            config.TeamCity.User = _teamCityUserName.Value();
            config.TeamCity.Password = _teamCityPassword.Value();

            if (string.IsNullOrEmpty(config.GitHub.AccessToken))
            {
                _reporter.Error("Must provide the Github AccessToken");
            }

            if (string.IsNullOrEmpty(config.Email.SmtpConfig.Login))
            {
                _reporter.Error("Must provide the SMTP Login");
            }

            if (string.IsNullOrEmpty(config.Email.SmtpConfig.Password))
            {
                _reporter.Error("Must provide the SMTP Password");
            }

            if (string.IsNullOrEmpty(config.TeamCity.User))
            {
                _reporter.Error("Must provide the TeamCity Username");
            }

            if (string.IsNullOrEmpty(config.TeamCity.Password))
            {
                _reporter.Error("Must provide the TeamCity Password");
            }

            return config;
        }

        protected override bool IsValid()
        {
            return true;
        }
    }

    public class Triage
    {
        public static string TriagedTag = "Triaged";

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
            var builds = GetUnTriagedFailures();
            _reporter.Output($"Let's triage!");
            var failedCount = 0;
            foreach (var build in builds)
            {
                failedCount++;
                _reporter.Output($"Triaging {build.WebURL}...");
                await HandleFailure(build);
            }
            stopWatch.Stop();

            _reporter.Output($"There were {failedCount} untriaged failures since {CutoffDate} and we handled them in {stopWatch.Elapsed.TotalMinutes} minutes. Let's get some coffee!");
        }

        private static readonly IEnumerable<HandleFailureBase> Handlers = new List<HandleFailureBase> { new HandleLowValueBuilds(), new HandleNonAllowedBuilds(),
                new HandleUniverseMovedOn(), new HandleTestFailures(),
                new HandleBuildTimeFailures(), new HandleUnhandled() };
        
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
