using System;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;

namespace E2ETests
{
    public class Helpers
    {
        public static string GetApplicationPath(ApplicationType applicationType)
        {
            var solutionDirectory = TestPathUtilities.GetSolutionRootDirectory("MusicStore");
            return Path.GetFullPath(Path.Combine(solutionDirectory, "samples", "MusicStore"));
        }

        public static string GetCurrentBuildConfiguration()
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
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

        public static string GetTargetFramework(RuntimeFlavor flavor)
        {
            if (flavor == RuntimeFlavor.Clr)
            {
                return "net461";
            }
            else if (flavor == RuntimeFlavor.CoreClr)
            {
#if NETCOREAPP2_0
                return "netcoreapp2.0";
#elif NETCOREAPP2_1
                return "netcoreapp2.1";
#else
#error Target Frameworks need to be updated
#endif
            }

            throw new ArgumentException($"Unknown runtime flavor '{flavor}.");
        }
    }
}