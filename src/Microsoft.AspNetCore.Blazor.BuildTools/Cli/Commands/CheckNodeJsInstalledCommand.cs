// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Blazor.BuildTools.Cli.Commands
{
    class CheckNodeJsInstalledCommand
    {
        private static Regex NodeVersionRegex = new Regex(@"^v(\d+\.\d+\.\d+)");

        public static void Command(CommandLineApplication command)
        {
            command.Description = "Asserts that Node.js is installed.";
            command.HelpOption("-?|-h|--help");

            var minVersionOption = command.Option(
                "-v|--version",
                "Specifies a minimum acceptable version of Node.js.",
                CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                var foundNodeVersion = GetInstalledNodeVersion();
                if (foundNodeVersion == null)
                {
                    return 1;
                }

                if (minVersionOption.HasValue())
                {
                    var minVersion = new Version(minVersionOption.Value());
                    if (foundNodeVersion < minVersion)
                    {
                        Console.WriteLine($"ERROR: The installed version of Node.js is too old. Required version: {minVersion}; Found version: {foundNodeVersion}.");
                        return 1;
                    }
                }

                Console.WriteLine($"Found Node.js version {foundNodeVersion}");
                return 0;
            });
        }

        private static Version GetInstalledNodeVersion()
        {
            var versionString = InvokeNodeVersionCommand();
            if (versionString == null)
            {
                return null;
            }

            var versionStringMatch = NodeVersionRegex.Match(versionString);
            if (!versionStringMatch.Success)
            {
                Console.WriteLine($"ERROR: Got unparseable Node.js version string: {versionStringMatch}");
                return null;
            }

            return new Version(versionStringMatch.Groups[1].Value);
        }

        private static string InvokeNodeVersionCommand()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "-v",
                    RedirectStandardOutput = true
                });
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"ERROR: The command 'node -v' exited with code {process.ExitCode}.");
                    return null;
                }
                else
                {
                    return process.StandardOutput.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Node.js was not found. Ensure that Node.js is installed and that 'node' is present on the system PATH.");
                Console.WriteLine("The underlying error was: " + ex.Message);
                return null;
            }
        }
    }
}
