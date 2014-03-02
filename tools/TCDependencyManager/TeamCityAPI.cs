using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TCDependencyManager
{
    public class TeamCityAPI
    {
        private readonly string _teamCityUrl;
        private readonly ICredentials _creds;

        public TeamCityAPI(string teamCityUrl, ICredentials creds)
        {
            _teamCityUrl = teamCityUrl;
            _creds = creds;
        }

        public bool TryGetDependencies(string configId, out List<string> dependencies)
        {
            string url = String.Format("httpAuth/app/rest/buildTypes/{0}/snapshot-dependencies", configId);
            var client = GetClient();
            var response = client.GetAsync(url).Result;
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // We don't have the config setup on the CI. That is ok.
                dependencies = null;
                return false;
            }
            dependencies = response.EnsureSuccessStatusCode()
                                   .Content.ReadAsAsync<SnapshotDependencies>()
                                   .Result
                                   .Dependencies.Select(f => f.Id)
                                   .ToList();
            return true;
        }

        public void SetDependencies(string configId, IEnumerable<string> dependencies)
        {
            foreach (var dependencyId in dependencies)
            {
                Console.WriteLine("For {0} adding: {1}", configId, dependencyId);

                string url = String.Format("httpAuth/app/rest/buildTypes/{0}/snapshot-dependencies", configId);
                var client = GetClient();
                var props = new Properties
                {
                    Property = new List<NameValuePair>
                    {
                        new NameValuePair("run-build-if-dependency-failed", "true"),
                        new NameValuePair("take-successful-builds-only", "true"),
                        new NameValuePair("take-started-build-with-same-revisions", "true")
                    }
                };

                var snapshotDependency = new SnapshotDepedency
                {
                    Id = dependencyId,
                    Type = "snapshot_dependency",
                    Properties = props,
                    BuildType = new BuildType
                    {
                        Id = dependencyId,
                        Name = dependencyId,
                        ProjectId = "AspNet",
                        ProjectName = "AspNet"
                    }
                };
                var serialized = JsonConvert.SerializeObject(snapshotDependency, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                var content = new StringContent(serialized);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = client.PostAsync(url, content).Result;
                response.EnsureSuccessStatusCode();
            }
        }

        private static string NormalizeId(string dependencyId)
        {
            return dependencyId.Replace(".", "");
        }

        public void EnsureDependencies(string configId, IEnumerable<string> dependencies)
        {
            List<string> currentDepenencies;
            if (TryGetDependencies(configId, out currentDepenencies))
            {
                var dependenciesToAdd = dependencies.Select(NormalizeId)
                                                    .Except(currentDepenencies, StringComparer.OrdinalIgnoreCase);
                
                SetDependencies(configId, dependenciesToAdd);
            }
        }

        private HttpClient GetClient()
        {
            var handler = new HttpClientHandler
            {
                PreAuthenticate = true,
                Credentials = _creds
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_teamCityUrl)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }


    }
}
