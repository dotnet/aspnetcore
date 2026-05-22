// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;

namespace HelixTestRunner;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var options = HelixTestRunnerOptions.Parse(args);
            var runner = new TestRunner(options);
            ProcessUtil.PrintMessage($"Configured {runner.Options.Targets.Length} test assembly(ies).");

            var keepGoing = runner.SetupEnvironment();
            if (keepGoing)
            {
                keepGoing = await runner.InstallDotnetToolsAsync();
            }

            if (keepGoing)
            {
                if (runner.Options.InstallPlaywright)
                {
                    keepGoing = runner.InstallPlaywright();
                }
                else
                {
                    ProcessUtil.PrintMessage("Playwright install skipped.");
                }
            }

            runner.DisplayContents();

            if (keepGoing)
            {
                if (!await runner.CheckTestDiscoveryAsync())
                {
                    ProcessUtil.PrintMessage("RunTest stopping due to test discovery failure.");
                    Environment.Exit(1);
                    return;
                }

                ProcessUtil.PrintMessage("Start running tests");
                var exitCode = await runner.RunTestsAsync();
                ProcessUtil.PrintMessage("Running tests complete");

                ProcessUtil.PrintMessage("Uploading test results");
                runner.UploadResults();
                ProcessUtil.PrintMessage("Test results uploaded");

                // For batched runs, always exit 0 so the Helix SDK reports all test results
                // to AzDO. Individual test failures are visible through the test results XML.
                // This avoids losing results from passing assemblies when one assembly fails,
                // and prevents wasteful retries of entire batches for a single flaky test.
                if (runner.Options.IsBatched && exitCode != 0)
                {
                    ProcessUtil.PrintMessage($"Batched run had test failures (exit code {exitCode}) but exiting 0 to ensure all results are reported.");
                    exitCode = 0;
                }

                ProcessUtil.PrintMessage($"Completed Helix job with exit code '{exitCode}'");
                Environment.Exit(exitCode);
            }

            ProcessUtil.PrintMessage("Tests were not run due to previous failures. Exit code=1");
            Environment.Exit(1);
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"HelixTestRunner uncaught exception: {e}");
            Environment.Exit(1);
        }
    }
}
