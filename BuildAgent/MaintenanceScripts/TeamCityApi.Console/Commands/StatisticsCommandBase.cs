// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Tools.Internal;
using Microsoft.Extensions.CommandLineUtils;

namespace TeamCityApi.Console.Commands
{
    internal abstract class StatisticsCommandBase : CommandBase
    {
        private CommandOption _ciServer;
        private CommandOption _ciUserName;
        private CommandOption _ciPassword;

        private CommandOption _verbose;

        public TeamCityClient Client => new TeamCityClient(_ciServer.Value(), _ciUserName.Value(), _ciPassword.Value(), Reporter);

        private CommandOption StartDateOption { get; set; }

        protected DateTime StartDate
        {
            get
            {
                if (StartDateOption.HasValue())
                {
                    return DateTime.Parse(StartDateOption.Value());
                }
                else
                {
                    return DateTime.Now.Subtract(TimeSpan.FromDays(7));
                }
            }
        }

        public const string OutputFileDescription = "The file to output stats to.";

        public IReporter Reporter => new ConsoleReporter(PhysicalConsole.Singleton, verbose: _verbose != null, quiet: false);

        protected override void ConfigureDefaultOptions(CommandLineApplication application)
        {
            _verbose = application.Option("-v|--verbose", "Show verbose output", CommandOptionType.NoValue);
            _ciServer = application.Option("--ci-server", "The server we're operating against", CommandOptionType.SingleValue);
            _ciUserName = application.Option("--ci-username", "The username to use for API access", CommandOptionType.SingleValue);
            _ciPassword = application.Option("--ci-password", "The password to use for API access", CommandOptionType.SingleValue);
            StartDateOption = application.Option("--start-date", "Return results from after this date", CommandOptionType.SingleValue);
        }

        protected override bool IsValid()
        {
            var isValid = true;

            if (_ciServer == null || !_ciServer.HasValue())
            {
                Reporter.Error("Must provide --ci-server");
                isValid = false;
            }

            if (_ciPassword == null || !_ciPassword.HasValue())
            {
                Reporter.Error("Must provide --ci-password");
                isValid = false;
            }

            if (_ciUserName == null || !_ciUserName.HasValue())
            {
                Reporter.Error("Must provide --ci-username");
                isValid = false;
            }

            if (StartDateOption.HasValue() && !DateTime.TryParse(StartDateOption.Value(), out var _))
            {
                Reporter.Error("--start-date must be formated as a DateTime");
                isValid = false;
            }

            return isValid;
        }
    }
}
