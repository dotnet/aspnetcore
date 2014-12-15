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

        public static bool SkipTestOnCurrentConfiguration(bool RunTestOnMono, KreArchitecture architecture, ServerType serverType)
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

            if (serverType == ServerType.IISNativeModule &&
                !(Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2))
            {
                // Works only on 6.2 and above
                Console.WriteLine("Skipping Native module test since machine is not Win2012R2/Win8.1 and above");
                return true;
            }

            //if (serverType == ServerType.IISNativeModule && 
            //    Environment.GetEnvironmentVariable("IIS_NATIVE_MODULE_SETUP") != "true")
            //{
            //    // Native module variations require IIS setup. Once native module is setup on IIS, set the value of the environment
            //    // variable to true to run the native module variation.
            //    // TODO: Need a better way to detect native module on the machine.
            //    Console.WriteLine("Skipping Native module test since native module is not installed on IIS.");
            //    Console.WriteLine("Setup the native module and set the IIS_NATIVE_MODULE_SETUP to true to run the variation.");
            //    return true;
            //}

            return false;
        }
    }
}