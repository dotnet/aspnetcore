// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.TeamCity
{
    public class TeamCityClientWrapper : ICIClient
    {
        private readonly IReporter _reporter;

        private const int _defaultCount = 1000000;

        public TeamCityConfig Config { get; private set; }

        public TeamCityClientWrapper(TeamCityConfig config, IReporter reporter)
        {
            Config = config;
            _reporter = reporter;

            if (TeamCityBuild.BuildNames == null)
            {
                TeamCityBuild.BuildNames = GetBuildTypes();
            }
        }

        public async Task<string> GetTestFailureText(ICITestOccurrence test)
        {
            var url = $"failedTestText.html?buildId={test.BuildId}&testId={test.TestId}";
            using (var stream = await MakeTeamCityRequest(HttpMethod.Get, url, timeout: TimeSpan.FromMinutes(1)))
            using (var reader = new StreamReader(stream))
            {
                var error = reader.ReadToEnd().Trim();
                error = HttpUtility.HtmlDecode(error);
                var lines = error.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                bool firstEqualLineSeen = false;

                var preservedLines = new List<string>();

                foreach (var line in lines)
                {
                    if (line.StartsWith("======= Failed test run", StringComparison.OrdinalIgnoreCase))
                    {
                        if (firstEqualLineSeen)
                        {
                            break;
                        }
                        else
                        {
                            firstEqualLineSeen = true;
                        }
                    }
                    else
                    {
                        preservedLines.Add(line);
                    }
                }

                return string.Join(Environment.NewLine, preservedLines);
            }
        }

        public async Task<IEnumerable<ICITestOccurrence>> GetTests(ICIBuild build, BuildStatus? buildStatus = null)
        {
            var locator = $"build:(id:{build.Id})";
            if (buildStatus != null)
            {
                locator += $",status:{Enum.GetName(typeof(BuildStatus), buildStatus)}";
            }
            var fields = "testOccurrence(test:id,id,name,status,duration)";

            var url = $"httpAuth/app/rest/testOccurrences?locator={locator},count:{_defaultCount}&fields={fields}";
            using (var stream = await MakeTeamCityRequest(HttpMethod.Get, url, timeout: TimeSpan.FromMinutes(5)))
            {
                var serializer = new XmlSerializer(typeof(TeamCityTestOccurrences));
                var tests = serializer.Deserialize(stream) as TeamCityTestOccurrences;

                var results = new List<TeamCityTestOccurrence>();

                foreach (var test in tests.TestList)
                {
                    test.BuildTypeId = build.BuildTypeID;
                }

                return tests.TestList;
            }
        }

        public async Task<IEnumerable<string>> GetTags(ICIBuild build)
        {
            var url = $"httpAuth/app/rest/builds/{build.Id}/tags";
            using (var stream = await MakeTeamCityRequest(HttpMethod.Get, url, timeout: TimeSpan.FromMinutes(1)))
            {
                var serializer = new XmlSerializer(typeof(TeamCityTags));
                var tags = serializer.Deserialize(stream) as TeamCityTags;

                return tags.TagList.Select(t => t.Name);
            }
        }

        public async Task SetTag(ICIBuild build, string tag)
        {
            var url = $"app/rest/builds/{build.Id}/tags/";
            (await MakeTeamCityRequest(HttpMethod.Post, url, tag)).Dispose();
        }

        public async Task<IEnumerable<ICIBuild>> GetFailedBuilds(DateTime startDate)
        {
            return await GetBuilds($"sinceDate:{TCDateTime(startDate)},status:FAILURE");
        }

        public async Task<IDictionary<string, string>> GetBuildTypes()
        {
            var fields = "buildType(id,name)";

            var url = $"httpAuth/app/rest/buildTypes?fields={fields}";
            using (var stream = await MakeTeamCityRequest(HttpMethod.Get, url))
            {
                var serializer = new XmlSerializer(typeof(TeamCityBuildTypes));
                var buildTypes = serializer.Deserialize(stream) as TeamCityBuildTypes;

                var result = new Dictionary<string, string>();
                foreach (var buildType in buildTypes.BuildTypeList)
                {
                    result.Add(buildType.Id, buildType.Name);
                }

                return result;
            }
        }

        public Task<IEnumerable<ICIBuild>> GetBuilds(DateTime startDate)
        {
            return GetBuilds($"sinceDate:{TCDateTime(startDate)}");
        }

        public async Task<IEnumerable<ICIBuild>> GetBuilds(string locator)
        {
            var fields = "build(id,startDate,buildTypeId,status,branchName,webUrl)";

            var url = $"httpAuth/app/rest/builds?locator={locator},count:{_defaultCount}&fields={fields}";
            using (var stream = await MakeTeamCityRequest(HttpMethod.Get, url))
            {
                var serializer = new XmlSerializer(typeof(TeamCityBuilds));
                var builds = serializer.Deserialize(stream) as TeamCityBuilds;

                return builds.BuildList;
            }
        }

        private async Task<Stream> MakeTeamCityRequest(HttpMethod method, string url, string body = null, TimeSpan? timeout = null)
        {
            var requestUri = $"http://{Config.Server}/{url}";

            var authInfo = $"{Config.User}:{Config.Password}";
            var authEncoded = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));

            using (var client = new HttpClient())
            {
                if (timeout != null)
                {
                    client.Timeout = timeout.Value;
                }

                var response = await RetryHelpers.RetryAsync(
                    async () =>
                    {
                        var request = new HttpRequestMessage(method, requestUri);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authEncoded);
                        if (body != null)
                        {
                            request.Content = new StringContent(body);
                        }
                        return await client.SendAsync(request);
                    },
                    _reporter);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsStreamAsync();
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _reporter.Error($"HTTP error: {response.StatusCode}");
                    _reporter.Error($"Content: {content}");
                    throw new HttpRequestException(response.StatusCode.ToString());
                }
            }
        }

        public static DateTimeOffset ParseTCDateTime(string date)
        {
            return DateTimeOffset.ParseExact(
                date,
                "yyyyMMdd'T'HHmmsszzz",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        public static string TCDateTime(DateTime date)
        {
            return date.ToString("yyyyMMddTHHmmss") + "-0800";
        }

        public async Task<string> GetBuildLog(ICIBuild build)
        {
            var buildLogDir = Path.Combine("temp", "BuildLogs");
            var buildLogFile = Path.Combine(buildLogDir, $"{build.Id}.txt");

            Directory.CreateDirectory(buildLogDir);
            if (!File.Exists(buildLogFile))
            {
                using (var fileStream = File.Create(buildLogFile))
                using (var stream = await MakeTeamCityRequest(HttpMethod.Get, $"httpAuth/downloadBuildLog.html?buildId={build.Id}"))
                {
                    stream.CopyTo(fileStream);
                }
            }

            return File.ReadAllText(buildLogFile);
        }
    }
}
