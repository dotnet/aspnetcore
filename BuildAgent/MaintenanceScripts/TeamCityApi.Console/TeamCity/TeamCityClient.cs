// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Tools.Internal;

namespace TeamCityApi
{
    public class TeamCityClient
    {
        private readonly string _ciServer;
        private readonly string _ciUserName;
        private readonly string _ciPassword;
        private readonly IReporter _reporter;
        private const int _defaultCount = 1000000;

        public TeamCityClient(string ciServer, string ciUserName, string ciPassword, IReporter reporter)
        {
            _ciServer = ciServer;
            _ciUserName = ciUserName;
            _ciPassword = ciPassword;
            _reporter = reporter;

            if (Build.BuildNames == null)
            {
                Build.BuildNames = GetBuildTypes();
            }
        }

        public IEnumerable<TestOccurrence> GetTests(int buildId, string buildTypeId)
        {
            var locator = $"build:(id:{buildId})";
            var fields = "testOccurrence(test:id,name,status,duration)";

            var url = $"httpAuth/app/rest/testOccurrences?locator={locator},count:{_defaultCount}&fields={fields}";
            using (var stream = MakeTeamCityRequest(url, TimeSpan.FromMinutes(5)))
            {
                var serializer = new XmlSerializer(typeof(TestOccurrences));
                var tests = serializer.Deserialize(stream) as TestOccurrences;

                foreach (var test in tests.TestList)
                {
                    test.BuildTypeId = buildTypeId;
                    yield return test;
                }
            }
        }

        public IList<Build> GetFailedBuilds(DateTime startDate)
        {
            return GetBuilds($"sinceDate:{TCDateTime(startDate)},status:FAILURE");
        }

        public IDictionary<string, string> GetBuildTypes()
        {
            var fields = "buildType(id,name)";

            var url = $"httpAuth/app/rest/buildTypes?fields={fields}";
            using (var stream = MakeTeamCityRequest(url))
            {
                var serializer = new XmlSerializer(typeof(BuildTypes));
                var buildTypes = serializer.Deserialize(stream) as BuildTypes;

                var result = new Dictionary<string, string>();
                foreach (var buildType in buildTypes.BuildTypeList)
                {
                    result.Add(buildType.Id, buildType.Name);
                }

                return result;
            }
        }

        public IList<Build> GetBuilds(DateTime startDate)
        {
            return GetBuilds($"sinceDate:{TCDateTime(startDate)}");
        }

        public IList<Build> GetBuilds(string locator)
        {
            var fields = "build(id,startDate,buildTypeId,status,branchName,webUrl)";

            var url = $"httpAuth/app/rest/builds?locator={locator},count:{_defaultCount}&fields={fields}";
            using (var stream = MakeTeamCityRequest(url))
            {
                var serializer = new XmlSerializer(typeof(Builds));
                var builds = serializer.Deserialize(stream) as Builds;

                return builds.BuildList;
            }
        }

        private Stream MakeTeamCityRequest(string url, TimeSpan? timeout = null)
        {
            var requestUri = $"http://{_ciServer}/{url}";

            var authInfo = $"{_ciUserName}:{_ciPassword}";
            var authEncoded = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authEncoded);

            using (var client = new HttpClient())
            {
                if (timeout != null)
                {
                    client.Timeout = timeout.Value;
                }

                var response = client.SendAsync(request).Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response.Content.ReadAsStreamAsync().Result;
                }
                else
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    _reporter.Error($"Http error: {response.StatusCode}");
                    _reporter.Error($"Content: {content}");
                    throw new HttpRequestException(response.StatusCode.ToString());
                }
            }
        }

        public static string TCDateTime(DateTime date)
        {
            return date.ToString("yyyyMMddTHHmmss") + "-0800";
        }
    }
}
