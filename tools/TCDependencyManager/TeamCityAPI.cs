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
        private const string TriggersEndPoint = "httpAuth/app/rest/buildTypes/{0}/triggers";
        private readonly string _teamCityUrl;
        private readonly ICredentials _creds;

        public TeamCityAPI(string teamCityUrl, ICredentials creds)
        {
            _teamCityUrl = teamCityUrl;
            _creds = creds;
        }

        public bool TryGetSnapshotDependencies(string configId, out List<string> dependencies)
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

        public List<Trigger> GetTriggers(string configId)
        {
            string url = String.Format(TriggersEndPoint, configId);
            var client = GetClient();
            var response = client.GetAsync(url).Result;
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // We don't have the config setup on the CI. That is ok.
                return null;
            }
            var triggers =  response.EnsureSuccessStatusCode()
                                    .Content.ReadAsAsync<Triggers>()
                                    .Result;

            return triggers.Trigger;
        }

        public void AddFinishTriggers(string configId, IEnumerable<string> finishConfigIds)
        {
            foreach (var finishConfigId in finishConfigIds)
            {
                var props = new Properties
                {
                    Property = new List<NameValuePair>
                    {
                        new NameValuePair("afterSuccessfulBuildOnly", "true"),
                        new NameValuePair("dependsOn", finishConfigId)
                    }
                };

                var trigger = new Trigger
                {
                    Id = "Trigger_" + finishConfigId,
                    Properties = props,
                    Type = "buildDependencyTrigger"
                };

                string url = String.Format(TriggersEndPoint, configId);
                var client = GetClient();
                var response = client.PostAsync(url, GetJsonContent(trigger)).Result;
                response.EnsureSuccessStatusCode();
            }
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

                var snapshotDependency = new SnapshotDependency
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
                var content = GetJsonContent(snapshotDependency);
                var response = client.PostAsync(url, content).Result;
                response.EnsureSuccessStatusCode();
            }
        }

        public void EnsureDependencies(string configId, IEnumerable<string> dependencies)
        {
            List<string> currentDependencies;
            if (TryGetSnapshotDependencies(configId, out currentDependencies))
            {
                dependencies = dependencies.Select(NormalizeId);

                var dependenciesToAdd = dependencies.Except(currentDependencies, StringComparer.OrdinalIgnoreCase);

                SetDependencies(configId, dependenciesToAdd);

                var currentTriggers = GetTriggers(configId)
                                            .Where(t => t.Type.Equals("buildDependencyTrigger", StringComparison.OrdinalIgnoreCase))
                                            .Select(t => t.Properties.Property.First(f => f.Name.Equals("dependsOn", StringComparison.OrdinalIgnoreCase)).Value);

                var triggersToAdd = dependencies.Except(currentTriggers);
                AddFinishTriggers(configId, triggersToAdd);
            }
        }

        private static StringContent GetJsonContent<TVal>(TVal value)
        {
            var serialized = JsonConvert.SerializeObject(value,
                                                         new JsonSerializerSettings
                                                         {
                                                             ContractResolver = new CamelCasePropertyNamesContractResolver()
                                                         });
            var content = new StringContent(serialized);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        private static string NormalizeId(string dependencyId)
        {
            return dependencyId.Replace(".", "");
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
