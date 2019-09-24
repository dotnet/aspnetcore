using System;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;

namespace E2ETests
{
    public class Helpers
    {
        public static string GetApplicationPath()
        {
            var solutionDirectory = TestPathUtilities.GetSolutionRootDirectory("MusicStore");
            return Path.GetFullPath(Path.Combine(solutionDirectory, "samples", "MusicStore"));
        }

        public static bool PreservePublishedApplicationForDebugging
        {
            get
            {
                var deletePublishedFolder = Environment.GetEnvironmentVariable("ASPNETCORE_DELETEPUBLISHEDFOLDER");

                if (string.Equals("false", deletePublishedFolder, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("0", deletePublishedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    // preserve the published folder and do not delete it
                    return true;
                }

                // do not preserve the published folder and delete it
                return false;
            }
        }

        public static string GetConfigContent(ServerType serverType, string iisConfig)
        {
            var applicationBasePath = AppContext.BaseDirectory;

            string content = null;
            if (serverType == ServerType.IISExpress)
            {
                content = File.ReadAllText(Path.Combine(applicationBasePath, iisConfig));
            }

            return content;
        }
    }
}
