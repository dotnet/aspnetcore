// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace TeamCityApi.Console.Commands
{
    internal class TestStatisticsCommand : StatisticsCommandBase
    {
        private CommandOption _output;

        public const string DefaultOutputFile = "teststats.csv";

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
            return TestStatistics(Client, StartDate, OutputFile);
        }

        public static int TestStatistics(TeamCityClient client, DateTime startDate, string outputFile = DefaultOutputFile)
        {
            var failedBuilds = client.GetFailedBuilds(startDate);

            // TODO: Ignore builds we don't own (WebSdk-Integration,WebSdk etc)
            var failedBuildTypes = new HashSet<string>(failedBuilds.Select(s => s.BuildTypeID), StringComparer.OrdinalIgnoreCase);

            var ts = new Dictionary<string, TestStats>();
            foreach (var buildType in failedBuildTypes)
            {
                var builds = client.GetBuilds($"buildType:{buildType},sinceDate:{TeamCityClient.TCDateTime(startDate)}");

                foreach (var build in builds)
                {
                    var tests = client.GetTests(build.Id, build.BuildTypeID);
                    foreach (var test in tests)
                    {
                        if (!test.Ignored)
                        {
                            if (!ts.TryGetValue(test.Key, out var stats))
                            {
                                stats = new TestStats(test);
                                ts.Add(test.Key, stats);
                            }

                            stats.ConsiderTest(test);
                        }
                    }
                }
            }

            using (var csv = new StreamWriter(outputFile))
            {
                csv.WriteLine("TestName,BuildTypeId,Count,Pass,Failed,Pass %");

                var failedStats = ts
                    .Select(kvp => kvp.Value)
                    .Where(s => s.PassPercent < 100)
                    .OrderBy(s => s.PassPercent);

                foreach (var stat in failedStats)
                {
                    csv.WriteLine($"{stat.Name.Replace(',', ':')},{stat.BuildTypeId},{stat.Count},{stat.Passed},{stat.Failed},{stat.PassPercent}");
                }
            }

            return 0;
        }

        private class TestStats
        {
            public string ID { get; }
            public string Name { get; }
            public string BuildTypeId { get; }
            public int Count { get; set; }
            public int Passed { get; set; }
            public int Failed { get; set; }
            public int Skipped { get; set; }

            public float PassPercent
            {
                get
                {
                    return ((float)Passed / Count) * 100;
                }
            }

            public TestStats(Test test)
            {
                ID = test.ID;
                Name = test.Name;
                BuildTypeId = test.BuildTypeId;
            }

            public void ConsiderTest(Test test)
            {
                switch (test.Status)
                {
                    case BuildStatus.SUCCESS:
                        Passed++;
                        break;
                    case BuildStatus.FAILURE:
                        Failed++;
                        break;
                    case BuildStatus.UNKNOWN:
                        Skipped++;
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled build status '{test.Status}'");
                }
            }
        }
    }
}
