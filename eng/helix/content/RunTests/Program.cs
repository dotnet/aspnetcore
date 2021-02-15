// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace RunTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var runner = new TestRunner(RunTestsOptions.Parse(args));

                var keepGoing = runner.SetupEnvironment();
                if (keepGoing)
                {
                    keepGoing = await runner.InstallDotnetDump();
                }
                if (keepGoing)
                {
                    keepGoing = await runner.InstallAspNetAppIfNeededAsync();
                }
                if (keepGoing)
                {
                    keepGoing = runner.InstallAspNetRefIfNeeded();
                }
                if (keepGoing)
                {
                    keepGoing = InstallPlaywrightIfNeededAsync();
                }

                runner.DisplayContents();

                if (keepGoing)
                {
                    if (!await runner.CheckTestDiscoveryAsync())
                    {
                        Console.WriteLine("RunTest stopping due to test discovery failure.");
                        Environment.Exit(1);
                        return;
                    }

                    var exitCode = await runner.RunTestsAsync();
                    runner.UploadResults();
                    Console.WriteLine($"Completed Helix job with exit code '{exitCode}'");
                    Environment.Exit(exitCode);
                }

                Console.WriteLine("Tests were not run due to previous failures. Exit code=1");
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RunTests uncaught exception: {e.ToString()}");
                Environment.Exit(1);
            }
        }
    }
}
