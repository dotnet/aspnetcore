using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.Extensions.Logging;

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

        public static string GetApplicationPath(ApplicationType applicationType)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", applicationType == ApplicationType.Standalone ? "MusicStore.Standalone" : "MusicStore"));
        }

        public static void SetInMemoryStoreForIIS(DeploymentParameters deploymentParameters, ILogger logger)
        {
            if (deploymentParameters.ServerType == ServerType.IIS)
            {
                // Can't use localdb with IIS. Setting an override to use InMemoryStore.
                logger.LogInformation("Creating configoverride.json file to override default config.");

                var compileRoot = Path.GetFullPath(
                    Path.Combine(
                        deploymentParameters.ApplicationPath,
                        "..", "approot", "packages", "MusicStore"));

                // We don't know the exact version number with which sources are built.
                string overrideConfig = Path.Combine(Directory.GetDirectories(compileRoot).First(), "root", "configoverride.json");


                File.WriteAllText(overrideConfig, "{\"UseInMemoryDatabase\": \"true\"}");
            }
        }
    }
}