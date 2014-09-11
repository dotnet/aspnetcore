using System;

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

        public static bool SkipTestOnCurrentConfiguration(bool RunTestOnMono, KreArchitecture architecture)
        {
            if (RunTestOnMono && !RunningOnMono)
            {
                //Skip Mono variations on Windows
                Console.WriteLine("Skipping mono variation on .NET");
                return true;
            }

            if (!RunTestOnMono && RunningOnMono)
            {
                //Skip .net variations on mono
                Console.WriteLine("Skipping .NET variation on mono");
                return true;
            }

            // Check if processor architecture is x64, else skip test
            if (architecture == KreArchitecture.amd64 && !Environment.Is64BitOperatingSystem)
            {
                Console.WriteLine("Skipping x64 test since machine is of type x86");
                return true;
            }

            return false;
        }
    }
}