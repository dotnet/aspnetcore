using System;
using System.IO;
using System.Net;
using System.Net.Http;
using DeploymentHelpers;
using Microsoft.Framework.Logging;

namespace E2ETests
{
    public class Helpers
    {
        public static bool RunningOnMono
        {
            get
            {
                return Type.GetType("Mono.Runtime") != null;
            }
        }

        public static string GetApplicationPath()
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "MusicStore"));
        }

        public static void SetInMemoryStoreForIIS(DeploymentParameters startParameters, ILogger logger)
        {
            if (startParameters.ServerType == ServerType.IIS
                || startParameters.ServerType == ServerType.IISNativeModule)
            {
                // Can't use localdb with IIS. Setting an override to use InMemoryStore.
                logger.LogInformation("Creating configoverride.json file to override default config.");
                var overrideConfig = Path.Combine(startParameters.ApplicationPath, "..", "approot", "src", "MusicStore", "configoverride.json");
                overrideConfig = Path.GetFullPath(overrideConfig);
                File.WriteAllText(overrideConfig, "{\"UseInMemoryStore\": \"true\"}");
            }
        }

        public static void ThrowIfResponseStatusNotOk(HttpResponseMessage response, ILogger _logger)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError(response.Content.ReadAsStringAsync().Result);
                throw new Exception(string.Format("Received the above response with status code : {0}", response.StatusCode));
            }
        }

        public static string PrefixBaseAddress(string url, ServerType serverType, string vDirName = null)
        {
#if DNX451
            url = (serverType == ServerType.IISNativeModule || serverType == ServerType.IIS) ?
                string.Format(url, vDirName) :
                string.Format(url, string.Empty);
#else
            url = string.Format(url, string.Empty);
#endif

            return url.Replace("//", "/").Replace("%2F%2F", "%2F").Replace("%2F/", "%2F");
        }
    }
}