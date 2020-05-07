// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
                    keepGoing = await runner.InstallAspNetAppIfNeededAsync();
                }
                if (keepGoing)
                {
                    keepGoing = runner.InstallAspNetRefIfNeeded();
                }

                runner.DisplayContents();

                if (keepGoing)
                {

                    _ = Task.Run(async () =>
                    {
                        Console.WriteLine("Waiting 22 minutes");
                        await Task.Delay(1320000);
                        Console.WriteLine("Done waiting");
                        try
                        {
                            var dumpDirectoryPath = Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");
                            Console.WriteLine($"Dump directory is {dumpDirectoryPath}");
                            var process = Process.GetCurrentProcess();
                            foreach (var dotnetProc in Process.GetProcessesByName("dotnet"))
                            {
                                Console.WriteLine($"Capturing dump of {dotnetProc.Id}");
                                if (dotnetProc.Id == process.Id)
                                    continue;

                                var dumpFilePath = Path.Combine(dumpDirectoryPath, $"{dotnetProc.ProcessName}-{dotnetProc.Id}.dmp");
                                await ProcessUtil.RunAsync($"{Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD")}/tools/dotnet-dump",
                                    $"collect -p {dotnetProc.Id} -o \"{dumpFilePath}\"",
                                    environmentVariables: runner.EnvironmentVariables,
                                    outputDataReceived: Console.WriteLine,
                                    errorDataReceived: Console.Error.WriteLine);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception getting dump(s) {ex}");
                        }
                    });

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
