using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TCDependencyManager
{
    public class GitHubAPI
    {
        private const string BaseUrl = "https://api.github.com/";
        private readonly string _oauthToken;

        public GitHubAPI(string oauthToken)
        {
            _oauthToken = oauthToken;
        }

        public List<Repository> GetRepos()
        {
            using (var client = GetClient())
            {
                var response = client.GetAsync("orgs/aspnet/repos?page=1&per_page=100").Result;
                return response.EnsureSuccessStatusCode()
                               .Content
                               .ReadAsAsync<List<Repository>>().Result;
            }
        }

        public List<Project> GetProjects(Repository repo)
        {
            IEnumerable<string> projectNames = null;
            using (var client = GetClient())
            {
                string path = string.Format("/repos/aspnet/{0}/contents/src?ref=dev", repo.Name);
                var response = client.GetAsync(path).Result;
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsAsync<JArray>().Result;
                    projectNames = result.Select(r => r["name"].Value<string>());
                }
                else
                {
                    projectNames = Enumerable.Empty<string>();
                }
            }
            return projectNames
                    .AsParallel()
                    .Select(p => new Project
                                 {
                                     Repo = repo,
                                     ProjectName = p,
                                     Dependencies = ReadDependencies(repo, p)
                                 })
                    .ToList();
        }

        private List<string> ReadDependencies(Repository repo, string project)
        {
            using (var client = GetClient())
            {
                string path = string.Format("/repos/aspnet/{0}/contents/src/{1}/project.json?ref=dev", repo.Name, project);
                var response = client.GetAsync(path).Result;
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsAsync<JObject>().Result;
                    var content = JsonConvert.DeserializeObject<JObject>(
                                    Encoding.UTF8.GetString(
                                        Convert.FromBase64String(result["content"].Value<string>())));
                    var dependencies = (JObject)content["dependencies"];
                    if (dependencies != null)
                    {
                        return dependencies.Cast<JProperty>()
                                       .Where(prop => !String.IsNullOrEmpty(prop.Value.Value<string>()))
                                       .Select(prop => prop.Name)
                                       .ToList();
                    }
                }
            }
            return new List<string>(0);
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AspNet-CI", "1.0"));
            client.DefaultRequestHeaders.Add("Authorization", "token " + _oauthToken);
            return client;
        }
    }
}
