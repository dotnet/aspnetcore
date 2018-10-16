// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TriageBuildFailures.Commands
{
    internal class RootCommand : CommandBase
    {
        private CommandOption _gitHubAccessToken;
        private CommandOption _teamCityUserName;
        private CommandOption _teamCityPassword;
        private CommandOption _smtpLogin;
        private CommandOption _smtpPassword;
        private CommandOption _vstsPAT;

        private IReporter _reporter;

        protected override void ConfigureCore(CommandLineApplication application)
        {
            _gitHubAccessToken = application.Option("-ghat|--github-access-token <ACCESSTOKEN>", "", CommandOptionType.SingleValue);
            _teamCityUserName = application.Option("-tcun|--team-city-username <TCUSERNAME>", "", CommandOptionType.SingleValue);
            _teamCityPassword = application.Option("-tcpw|--team-city-password <TCPASSWORD>", "", CommandOptionType.SingleValue);
            _smtpLogin = application.Option("-sl|--smtp-login <SMTPLOGIN>", "", CommandOptionType.SingleValue);
            _smtpPassword = application.Option("-sp|--smtp-password <SMTPPASSWORD>", "", CommandOptionType.SingleValue);
            _vstsPAT = application.Option("-vp|--vsts-pat <VSTSPAT>", "", CommandOptionType.SingleValue);
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
            catch (Exception ex)
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
            config.VSTS.PersonalAccessToken = _vstsPAT.Value();

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

            if (string.IsNullOrEmpty(config.VSTS.PersonalAccessToken))
            {
                _reporter.Error("Must provide the VSTS PAT");
            }

            if (string.IsNullOrEmpty(config.VSTS.Account))
            {
                _reporter.Error("Must provide the VSTS Account");
            }

            return config;
        }

        protected override bool IsValid()
        {
            return true;
        }
    }
}
