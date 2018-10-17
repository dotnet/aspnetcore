// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace TeamCityApi.Console.Commands
{
    internal class BuildStatisticsCommand : StatisticsCommandBase
    {
        public const string DefaultOutputFile = "buildstats.csv";

        private CommandOption _output;

        public string OutputFile
        {
            get
            {
                return _output != null && _output.HasValue() ? _output.Value() : DefaultOutputFile;
            }
        }

        protected override void ConfigureCore(CommandLineApplication application)
        {
            _output = application.Option("-o|--output-file <OUTPUTFILE>", OutputFileDescription, CommandOptionType.SingleValue);
        }

        protected override int Execute()
        {
            return BuildStatistics(Client, StartDate, OutputFile);
        }

        public static int BuildStatistics(TeamCityClient client, DateTime startDate, string outputFile = DefaultOutputFile)
        {
            if (outputFile == null)
            {
                throw new ArgumentNullException(nameof(outputFile));
            }

            // TODO: Ignore builds we don't own (WebSdk-Integration,WebSdk etc)
            var builds = client.GetBuilds(startDate);

            if (builds == null)
            {
                return 1;
            }

            var stats = new Dictionary<string, BuildStats>();

            foreach (var build in builds)
            {
                if (!stats.TryGetValue(build.Key, out var stat))
                {
                    stat = new BuildStats(build);
                    stats.Add(build.Key, stat);
                }

                stat.ConsiderBuild(build);
            }

            using (var csv = new StreamWriter(outputFile))
            {
                csv.WriteLine("Build,BuildTypeId,Branch,Count,Pass,Failed,Unknown,Pass %");

                var failedStats = stats
                    .Select(kvp => kvp.Value)
                    .Where(s => s.PassPercent < 100)
                    .OrderBy(s => s.PassPercent);

                foreach (var stat in failedStats)
                {
                    csv.WriteLine($"{stat.BuildName},{stat.BuildTypeId},{stat.Branch},{stat.Count},{stat.Passed},{stat.Failed},{stat.Unknown},{stat.PassPercent}%");
                }
            }

            return 0;
        }

        private class BuildStats
        {
            public string BuildName { get; }
            public string BuildTypeId { get; }
            public string Branch { get; }
            public int Count
            {
                get
                {
                    return Passed + Failed + Unknown;
                }
            }
            public int Passed { get; set; }
            public int Failed { get; set; }
            public int Unknown { get; set; }
            public float PassPercent
            {
                get
                {
                    return ((float)Passed / Count) * 100;
                }
            }

            public BuildStats(Build build)
            {
                BuildName = build.BuildName;
                BuildTypeId = build.BuildTypeID;
                Branch = build.BranchName;
            }

            public void ConsiderBuild(Build build)
            {
                switch (build.Status)
                {
                    case BuildStatus.SUCCESS:
                        Passed++;
                        break;
                    case BuildStatus.FAILURE:
                        Failed++;
                        break;
                    case BuildStatus.UNKNOWN:
                        Unknown++;
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled build status '{build.Status}'");
                }
            }
        }
    }
}
