// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;

namespace TeamCityApi.Console.Commands
{
    internal class AllStatisticsCommand : StatisticsCommandBase
    {
        private CommandOption _testOutputFile;

        private CommandOption _buildOutputFile;

        protected override void ConfigureCore(CommandLineApplication application)
        {
            _testOutputFile = application.Option("-t|--test-output-file <TESTOUTPUTFILE>", OutputFileDescription, CommandOptionType.SingleValue);
            _buildOutputFile = application.Option("-b|--build-output-file <BUILDOUTPUTFILE>", OutputFileDescription, CommandOptionType.SingleValue);
        }

        protected override int Execute()
        {
            var buildResult = BuildStatisticsCommand.BuildStatistics(Client, StartDate, _buildOutputFile.HasValue() ? _buildOutputFile.Value() : BuildStatisticsCommand.DefaultOutputFile);
            var testResult = TestStatisticsCommand.TestStatistics(Client, StartDate, _testOutputFile.HasValue() ? _testOutputFile.Value() : TestStatisticsCommand.DefaultOutputFile);

            return buildResult + testResult;
        }
    }
}
