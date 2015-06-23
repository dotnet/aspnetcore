using System;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Server.Testing;
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

                string overrideConfig;
                if (deploymentParameters.PublishWithNoSource)
                {
                    var compileRoot = Path.GetFullPath(
                        Path.Combine(
                            deploymentParameters.ApplicationPath,
                            "..", "approot", "packages", "MusicStore"));

                    // We don't know the exact version number with which sources are built.
                    overrideConfig = Path.Combine(Directory.GetDirectories(compileRoot).First(), "root", "configoverride.json");
                }
                else
                {
                    overrideConfig = Path.GetFullPath(
                        Path.Combine(
                            deploymentParameters.ApplicationPath,
                            "..", "approot", "src", "MusicStore", "configoverride.json"));
                }

                File.WriteAllText(overrideConfig, "{\"UseInMemoryDatabase\": \"true\"}");
            }
        }
    }
}