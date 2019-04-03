// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.VSTS.Models;

namespace TriageBuildFailures.VSTS
{
    public class VSTSBuildClient : VSTSClientBase, ICIClient
    {
        public VSTSBuildClient(VSTSConfig vstsConfig, IReporter reporter)
            : base(vstsConfig, reporter)
        {
        }

        public async Task<IEnumerable<ICIBuild>> GetFailedBuildsAsync(DateTime startDate)
        {
            var failedBuilds = await GetBuildsAsync(startDate, VSTSBuildResult.Failed);
            return failedBuilds.Concat(await GetBuildsAsync(startDate, VSTSBuildResult.PartiallySucceeded));
        }

        public async Task<IEnumerable<ICIBuild>> GetBuildsAsync(DateTime startDate, VSTSBuildResult result)
        {
            var projects = await GetProjects();
            var results = new List<ICIBuild>();
            foreach (var project in projects)
            {
                var builds = await GetBuildsForProject(project, result, VSTSBuildStatus.Completed, startDate);

                results.AddRange(builds.Select(b => new VSTSBuild(b)));
            }

            return results;
        }

        public async Task<IEnumerable<string>> GetTagsAsync(ICIBuild build)
        {
            var vstsBuild = (VSTSBuild)build;
            var result = await MakeVSTSRequest<VSTSArray<string>>(HttpMethod.Get, $"{vstsBuild.Project}/_apis/build/builds/{build.Id}/tags");

            return result.Value;
        }

        public Task<string> GetBuildLogAsync(ICIBuild build)
        {
            return GetBuildLogAsync(build, onlyFailures: true);
        }

        private static readonly List<VSTSTaskResult> SuccessResults = new List<VSTSTaskResult>{
            VSTSTaskResult.Succeeded,
            VSTSTaskResult.SucceededWithIssues
        };

        public async Task<string> GetBuildLogAsync(ICIBuild build, bool onlyFailures = true)
        {
            var timeline = await GetTimelineAsync(build);
            var builder = new StringBuilder();
            var vstsBuild = (VSTSBuild)build;
            var logs = await MakeVSTSRequest<VSTSArray<VSTSBuildLog>>(HttpMethod.Get, $"{vstsBuild.Project}/_apis/build/builds/{build.Id}/logs");
            var validationResults = GetBuildLogFromValidationResult(vstsBuild);
            if (logs == null)
            {
                return validationResults;
            }
            else
            {
                foreach (var record in timeline.Records)
                {
                    if (record.Result != null && (!SuccessResults.Contains(record.Result.Value) || !onlyFailures) && record.Log != null)
                    {
                        using (var stream = await MakeVSTSRequest(HttpMethod.Get, $"{vstsBuild.Project}/_apis/build/builds/{build.Id}/logs/{record.Log.Id}", "text/plain"))
                        using (var streamReader = new StreamReader(stream))
                        {
                            builder.Append(await streamReader.ReadToEndAsync());
                        }
                    }
                }

                var result = builder.ToString();

                if (string.IsNullOrEmpty(result))
                {
                    return validationResults;
                }
                else
                {
                    return result;
                }
            }
        }

        public async Task<VSTSTimeline> GetTimelineAsync(ICIBuild build)
        {
            var vstsBuild = (VSTSBuild)build;
            return await MakeVSTSRequest<VSTSTimeline>(HttpMethod.Get, $"{vstsBuild.Project}/_apis/build/builds/{build.Id}/timeline");
        }

        public Task<string> GetTestFailureTextAsync(ICITestOccurrence failure)
        {
            var vstsTest = failure as VSTSTestOccurrence;

            return Task.FromResult(vstsTest.TestCaseResult.ErrorMessage);
        }

        public async Task<IEnumerable<ICITestOccurrence>> GetTestsAsync(ICIBuild build, BuildStatus? buildStatus = null)
        {
            var runs = await GetTestRuns(build);

            var result = new List<ICITestOccurrence>();
            foreach (var run in runs)
            {
                var results = await GetTestResults(run, buildStatus);
                result.AddRange(results.Select(r => new VSTSTestOccurrence(r)));
            }

            return result;
        }

        public async Task SetTagAsync(ICIBuild build, string tag)
        {
            var vstsBuild = (VSTSBuild)build;
            await MakeVSTSRequest<VSTSArray<string>>(HttpMethod.Put, $"{vstsBuild.Project}/_apis/build/builds/{build.Id}/tags/{tag}", apiVersion: ApiVersion.V5_0_Preview2);
        }

        private string GetBuildLogFromValidationResult(VSTSBuild vstsBuild)
        {
            if (vstsBuild.ValidationResults != null && vstsBuild.ValidationResults.Any(v => v.Result.Equals("error", StringComparison.OrdinalIgnoreCase)))
            {
                var logStr = "";
                foreach (var validationResult in vstsBuild.ValidationResults.Where(v => v.Result.Equals("error", StringComparison.OrdinalIgnoreCase)))
                {
                    logStr += validationResult.Message + Environment.NewLine;
                }
                return logStr;
            }
            else
            {
                return null;
            }
        }

        private async Task<IEnumerable<VSTSTestRun>> GetTestRuns(ICIBuild build)
        {
            var vstsBuild = (VSTSBuild)build;
            var queryItems = new Dictionary<string, string>
            {
                { "buildUri", vstsBuild.Uri.ToString() },
                { "includeRunDetails", "true" }
            };

            var runs = await MakeVSTSRequest<VSTSArray<VSTSTestRun>>(HttpMethod.Get, $"{vstsBuild.Project}/_apis/test/runs", queryItems);
            return runs.Value;
        }

        private async Task<IEnumerable<VSTSTestCaseResult>> GetTestResults(VSTSTestRun run, BuildStatus? buildResult = null)
        {
            var queryItems = new Dictionary<string, string>();

            if (buildResult != null)
            {
                queryItems.Add("outcomes", GetStatusString(buildResult.Value));
            }

            var results = await MakeVSTSRequest<VSTSArray<VSTSTestCaseResult>>(HttpMethod.Get, $"{run.Project.Id}/_apis/test/runs/{run.Id}/results", queryItems, ApiVersion.V5_0_Preview5);

            return results.Value;
        }

        private string GetStatusString(BuildStatus status)
        {
            switch (status)
            {
                case BuildStatus.FAILURE:
                    return "Failed";
                case BuildStatus.SUCCESS:
                    return "Passed";
                default:
                    throw new NotImplementedException($"We don't know what to do with {Enum.GetName(typeof(BuildStatus), status)}");

            }
        }
    }
}
