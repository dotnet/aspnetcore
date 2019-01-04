// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using McMaster.Extensions.CommandLineUtils;

namespace TriageBuildFailures
{
    internal static class TeamCityReporterExtensions
    {
        /// <summary>
        /// Write out a build statistic value for TeamCity.
        /// More info here: https://confluence.jetbrains.com/display/TCD10/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ReportingBuildStatistics
        /// </summary>
        /// <param name="reporter"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void LogTeamCityStatistic(this IReporter reporter, string key, int value)
        {
            LogTeamCityStatistic(reporter, key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Write out a build statistic value for TeamCity.
        /// More info here: https://confluence.jetbrains.com/display/TCD10/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ReportingBuildStatistics
        /// </summary>
        /// <param name="reporter"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void LogTeamCityStatistic(this IReporter reporter, string key, float value)
        {
            LogTeamCityStatistic(reporter, key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Write out a build statistic value for TeamCity.
        /// More info here: https://confluence.jetbrains.com/display/TCD10/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ReportingBuildStatistics
        /// </summary>
        /// <param name="reporter"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void LogTeamCityStatistic(this IReporter reporter, string key, double value)
        {
            LogTeamCityStatistic(reporter, key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Write out a build statistic value for TeamCity.
        /// More info here: https://confluence.jetbrains.com/display/TCD10/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ReportingBuildStatistics
        /// </summary>
        /// <param name="reporter"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void LogTeamCityStatistic(this IReporter reporter, string key, decimal value)
        {
            LogTeamCityStatistic(reporter, key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Write out a build statistic value for TeamCity.
        /// More info here: https://confluence.jetbrains.com/display/TCD10/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ReportingBuildStatistics
        /// </summary>
        /// <param name="reporter"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void LogTeamCityStatistic(this IReporter reporter, string key, string value)
        {
            reporter.Output($"##teamcity[buildStatisticValue key='{key}' value='{value}']");
        }

    }
}
