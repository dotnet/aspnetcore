using System;
using System.IO;
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

        public static void SetInMemoryStoreForIIS(DeploymentParameters deploymentParameters, ILogger logger)
        {
            if (deploymentParameters.ServerType == ServerType.IIS
                || deploymentParameters.ServerType == ServerType.IISNativeModule)
            {
                // Can't use localdb with IIS. Setting an override to use InMemoryStore.
                logger.LogInformation("Creating configoverride.json file to override default config.");

                var overrideConfig = deploymentParameters.PublishWithNoSource ?
                Path.Combine(deploymentParameters.ApplicationPath, "..", "approot", "packages", "MusicStore", "1.0.0", "root", "configoverride.json") :
                Path.Combine(deploymentParameters.ApplicationPath, "..", "approot", "src", "MusicStore", "configoverride.json");

                overrideConfig = Path.GetFullPath(overrideConfig);
                File.WriteAllText(overrideConfig, "{\"UseInMemoryStore\": \"true\"}");
            }
        }
    }
}