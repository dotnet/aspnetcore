// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildPerformanceTest : MSBuildIntegrationTestBase, IClassFixture<BuildServerTestFixture>
    {
        public BuildPerformanceTest(BuildServerTestFixture buildServer)
            : base(buildServer)
        {
        }

        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task BuildMvcApp()
        {
            var result = await DotnetMSBuild(target: default, args: "/clp:PerformanceSummary");

            Assert.BuildPassed(result);
            var summary = ParseTaskPerformanceSummary(result.Output);

            Assert.Equal(1, summary.First(f => f.Name == "RazorGenerate").Calls);
            Assert.Equal(1, summary.First(f => f.Name == "RazorTagHelper").Calls);

            // Incremental builds
            for (var i = 0; i < 2; i++)
            {
                result = await DotnetMSBuild(target: default, args: "/clp:PerformanceSummary");

                Assert.BuildPassed(result);
                summary = ParseTaskPerformanceSummary(result.Output);

                Assert.DoesNotContain(summary, item => item.Name == "RazorGenerate");
                Assert.DoesNotContain(summary, item => item.Name == "RazorTagHelper");
            }
        }

        [Fact]
        [InitializeTestProject("MvcWithComponents")]
        public async Task BuildMvcAppWithComponents()
        {
            var result = await DotnetMSBuild(target: default, args: "/clp:PerformanceSummary");

            Assert.BuildPassed(result);
            var summary = ParseTaskPerformanceSummary(result.Output);

            // One for declaration build, one for the "real" code gen
            Assert.Equal(2, summary.First(f => f.Name == "RazorGenerate").Calls);
            Assert.Equal(1, summary.First(f => f.Name == "RazorTagHelper").Calls);

            // Incremental builds
            for (var i = 0; i < 2; i++)
            {
                result = await DotnetMSBuild(target: default, args: "/clp:PerformanceSummary");

                Assert.BuildPassed(result);
                summary = ParseTaskPerformanceSummary(result.Output);

                Assert.DoesNotContain(summary, item => item.Name == "RazorGenerate");
                Assert.DoesNotContain(summary, item => item.Name == "RazorTagHelper");
            }
        }

        private List<PerformanceSummaryEntry> ParseTaskPerformanceSummary(string output)
        {
            const string Header = "Task Performance Summary:";
            var lines = output.Split(Environment.NewLine);
            var taskSection = Array.LastIndexOf(lines, Header);
            Assert.True(taskSection != -1, $"Could not find line ${Header} in {output}");

            var entries = new List<PerformanceSummaryEntry>();
            // 6 ms  FindAppConfigFile                          4 calls
            var matcher = new Regex(@"\s+(?<time>\w+) ms\s+(?<name>\w+)\s+(?<calls>\d+) calls");
            for (var i = taskSection + 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var match = matcher.Match(lines[i]);
                Assert.True(match.Success, $"Line {lines[i]} did not match.");

                var entry = new PerformanceSummaryEntry
                {
                    Name = match.Groups["name"].Value,
                    Calls = int.Parse(match.Groups["calls"].Value),
                };
                entries.Add(entry);
            }

            return entries;
        }

        private class PerformanceSummaryEntry
        {
            public string Name { get; set; }

            public int Calls { get; set; }
        }
    }
}