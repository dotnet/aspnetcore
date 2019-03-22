// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.VSTS.Models;

namespace TriageBuildFailures.VSTS
{
    public abstract class VSTSClientBase
    {
        protected enum ApiVersion
        {
            V4_1_Preview2,
            V5_0_Preview,
            V5_0_Preview2,
            V5_0_Preview3,
            V5_0_Preview4,
            V5_0_Preview5,
            Default = V4_1_Preview2,
        }

        private readonly VSTSConfig Config;
        private readonly IReporter _reporter;

        public VSTSClientBase(VSTSConfig vstsConfig, IReporter reporter)
        {
            Config = vstsConfig;
            _reporter = reporter;
        }

        public async Task<VSTSBuild> GetBuild(string url)
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var id = query.Get("buildId");
            string project;
            if (url.Contains("dev.azure.com"))
            {
                project = url.Split('/', StringSplitOptions.RemoveEmptyEntries)[3];
            }
            else if (url.Contains("dnceng.visualstudio.com"))
            {
                project = url.Split('/', StringSplitOptions.RemoveEmptyEntries)[2];
            }
            else
            {
                throw new NotImplementedException("Unsupported url format");
            }
            var vstsUri = $"{project}/_apis/build/builds/{id}";
            var build = await MakeVSTSRequest<Build>(HttpMethod.Get, vstsUri, apiVersion: ApiVersion.V5_0_Preview5);
            return new VSTSBuild(build);
        }

        protected async Task<IEnumerable<Build>> GetBuildsForProject(VSTSProject project, VSTSBuildResult? result = null, VSTSBuildStatus? status = null, DateTime? minTime = null)
        {
            var queryItems = new Dictionary<string, string>();
            if (result != null)
            {
                queryItems["resultFilter"] = Enum.GetName(typeof(VSTSBuildResult), result.Value).ToLowerInvariant();
            }

            if (status != null)
            {
                queryItems["statusFilter"] = Enum.GetName(typeof(BuildStatus), status.Value).ToLowerInvariant();
            }

            if (minTime != null)
            {
                queryItems["minTime"] = ToVSTSString(minTime.Value);
            }
            var builds = (await MakeVSTSRequest<VSTSArray<Build>>(HttpMethod.Get, $"{project.Id}/_apis/build/builds", queryItems, ApiVersion.V5_0_Preview4)).Value;

            // Only look at aspnet builds, and ignore PR builds.
            return builds.Where(build =>
               build.Definition.Path.StartsWith(Config.BuildPath, StringComparison.OrdinalIgnoreCase) && !build.TriggerInfo.ContainsKey("pr.sourceBranch"));
        }

        protected string ToVSTSString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy'-'MM'-'ddTHH':'mm':'ss'Z'");
        }

        protected async Task<IEnumerable<VSTSProject>> GetProjects()
        {
            var projectsObj = await MakeVSTSRequest<VSTSArray<VSTSProject>>(HttpMethod.Get, "_apis/projects");
            return projectsObj.Value;
        }

        protected async Task<T> MakeVSTSRequest<T>(HttpMethod verb, string uri, IDictionary<string, string> queryItems = null, ApiVersion apiVersion = ApiVersion.Default, ApiType apiType = ApiType.Basic) where T : class
        {
            using (var stream = await MakeVSTSRequest(verb, uri, "application/json", queryItems, apiVersion, apiType))
            using (var sr = new StreamReader(stream))
            using (var reader = new JsonTextReader(sr))
            {
                var serializer = new JsonSerializer
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                return serializer.Deserialize<T>(reader);
            }
        }

        private string GetVersionString(ApiVersion version)
        {
            string result;
            switch (version)
            {
                case ApiVersion.V4_1_Preview2:
                    result = "4.1-preview.2";
                    break;
                case ApiVersion.V5_0_Preview:
                    result = "5.0-preview";
                    break;
                case ApiVersion.V5_0_Preview2:
                    result = "5.0-preview.2";
                    break;
                case ApiVersion.V5_0_Preview4:
                    result = "5.0-preview.4";
                    break;
                case ApiVersion.V5_0_Preview5:
                    result = "5.0-preview.5";
                    break;
                default:
                    throw new NotImplementedException($"We don't know about enum {Enum.GetName(typeof(ApiVersion), version)}.");
            }

            return result;
        }

        protected async Task<Stream> MakeVSTSRequest(
            HttpMethod verb,
            string uri,
            string accept,
            IDictionary<string, string> queryItems = null,
            ApiVersion apiVersion = ApiVersion.Default,
            ApiType apiType = ApiType.Basic)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["api-version"] = GetVersionString(apiVersion);

            if (queryItems != null)
            {
                foreach (var kvp in queryItems)
                {
                    query[kvp.Key] = kvp.Value;
                }
            }

            var url = GetUri(apiType, uri, query);

            return await HitVSTSUrlAsync(verb, url, accept);
        }

        protected async Task<Stream> HitVSTSUrlAsync(HttpMethod verb, Uri uri, string accept)
        {
            var credentials = GetCredentials();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["api-version"] = GetVersionString(apiVersion);

                if (queryItems != null)
                {
                    foreach (var kvp in queryItems)
                    {
                        query[kvp.Key] = kvp.Value;
                    }
                }
                var uriBuilder = new UriBuilder("https", $"{Config.Account}.visualstudio.com")
                {
                    Path = uri,
                    Query = query.ToString()
                };

                var response = await RetryHelpers.RetryHttpRequestAsync(client, verb, uriBuilder.Uri, _reporter);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
                    writer.Write(content);
                    writer.Flush();
                    stream.Position = 0;

                    return stream;
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == HttpStatusCode.InternalServerError && content.Contains("TF400714"))
                    {
                        //The log is missing. This is VSTS's fault, nothing we can do.
                        _reporter.Warn($"The log {uri} is missing! This is VSTS' fault.");
                        var stream = new MemoryStream();
                        var writer = new StreamWriter(stream);
                        writer.Write("");
                        writer.Flush();
                        stream.Position = 0;

                        return stream;
                    }
                    else
                    {
                        throw new HttpRequestException(content);
                    }
                }
            }
        }

        private Uri GetUri(ApiType apiType, string uri, NameValueCollection query)
        {
            string host;
            switch (apiType)
            {
                case ApiType.Basic:
                    host = $"{Config.Account}.visualstudio.com";
                    break;
                case ApiType.VSRM:
                    host = $"vsrm.dev.azure.com";
                    uri = $"{Config.Account}/{uri}";
                    break;
                default:
                    throw new NotImplementedException();
            };

            var uriBuilder = new UriBuilder("https", host)
            {
                Path = uri,
                Query = query.ToString()
            };

            return uriBuilder.Uri;
        }

        private string GetCredentials()
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", Config.PersonalAccessToken)));
        }

        protected enum ApiType
        {
            Basic,
            VSRM
        }
    }
}
