// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Interop.FunctionalTests
{
    public static class H2SpecCommands
    {
        private static string GetToolLocation()
        {
            var root = Path.Combine(Environment.CurrentDirectory, "h2spec");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(root, "windows", "h2spec.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(root, "linux", "h2spec");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(root, "darwin", "h2spec");
            }
            throw new NotImplementedException("Invalid OS");
        }

        public static IList<Tuple<string, string>> EnumerateTestCases()
        {
            var testCases = new List<Tuple<string, string>>();
            var processOptions = new ProcessStartInfo
            {
                FileName = GetToolLocation(),
                RedirectStandardOutput = true,
                Arguments = "--strict --dryrun",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };
            using (var process = Process.Start(processOptions))
            {
                // https://github.com/summerwind/h2spec#running-a-specific-test-case
                //Hypertext Transfer Protocol Version 2(HTTP / 2)
                //  3.Starting HTTP / 2
                //    3.5.HTTP / 2 Connection Preface
                //      1: Sends client connection preface
                //      2: Sends invalid connection preface
                //Generic tests for HTTP / 2 server
                //  1.Starting HTTP / 2
                //    1: Sends a client connection preface

                // Expected output: "http2/3.5/1", "Sends client connection preface"
                var groupName = string.Empty; // http2, generic, or hpack
                var sectionId = string.Empty; // 3 or 3.5

                var line = string.Empty;
                while (line != null)
                {
                    line = process.StandardOutput.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    if (IsGroupLine(line, out var group))
                    {
                        groupName = group;
                        continue;
                    }

                    if (IsSectionLine(line, out var section))
                    {
                        sectionId = section;
                        continue;
                    }

                    if (IsTestLine(line, out var testNumber, out var description))
                    {
                        testCases.Add(new Tuple<string, string>($"{groupName}/{sectionId}/{testNumber}", description));
                        continue;
                    }

                    throw new InvalidOperationException("Unrecognized line: " + line);
                }
            }
            return testCases;
        }

        private static bool IsGroupLine(string line, out string groupName)
        {
            if (line.StartsWith(" "))
            {
                groupName = null;
                return false;
            }

            if (line.StartsWith("Hypertext"))
            {
                groupName = "http2";
                return true;
            }
            if (line.StartsWith("Generic"))
            {
                groupName = "generic";
                return true;
            }
            if (line.StartsWith("HPACK"))
            {
                groupName = "hpack";
                return true;
            }
            throw new InvalidOperationException("Unrecognized line: " + line);
        }

        // "8.1.2.1. Pseudo-Header Fields"
        private static bool IsSectionLine(string line, out string section)
        {
            line = line.TrimStart();
            var firstSpace = line.IndexOf(" ");
            if (firstSpace < 2) // Minimum: "8. description"
            {
                section = string.Empty;
                return false;
            }

            // As opposed to test cases that are marked with :
            if (line[firstSpace - 1] == '.')
            {
                section = line.Substring(0, firstSpace - 1); // Drop the trailing dot.
                return true;
            }

            section = string.Empty;
            return false;
        }

        // "1: Sends a DATA frame"
        private static bool IsTestLine(string line, out string testNumber, out string description)
        {
            line = line.TrimStart();
            var firstSpace = line.IndexOf(" ");
            if (firstSpace < 2) // Minimum: "8: description"
            {
                testNumber = string.Empty;
                description = string.Empty;
                return false;
            }

            // As opposed to test cases that are marked with :
            if (line[firstSpace - 1] == ':')
            {
                testNumber = line.Substring(0, firstSpace - 1); // Drop the trailing colon.
                description = line.Substring(firstSpace + 1);
                return true;
            }

            testNumber = string.Empty;
            description = string.Empty;
            return false;
        }

        public static void RunTest(string testId, int port, bool https, ILogger logger)
        {
            var tempFile = Path.GetTempPath() + Guid.NewGuid() + ".xml";
            var processOptions = new ProcessStartInfo
            {
                FileName = GetToolLocation(),
                RedirectStandardOutput = true,
                Arguments = $"{testId} -p {port.ToString(CultureInfo.InvariantCulture)} --strict -j {tempFile} --timeout 15"
                    + (https ? " --tls --insecure" : ""),
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(processOptions))
            {
                var data = process.StandardOutput.ReadToEnd();
                logger.LogDebug(data);

                var results = File.ReadAllText(tempFile);
                File.Delete(tempFile);

                var xml = new XmlDocument();
                xml.LoadXml(results);
                // <testsuites>
                //     <testsuite name="4.2. Maximum Table Size" package="hpack/4.2" id="4.2" tests="1" skipped="0" failures="0" errors="1">
                var foundTests = false;
                var failures = new List<string>();
                foreach (XmlNode node in xml.GetElementsByTagName("testsuite"))
                {
                    if (node.Attributes["errors"].Value != "0")
                    {
                        // This does not list the individual sub-tests in each section
                        failures.Add("Test failed: " + node.Attributes["package"].Value + "; "  + node.Attributes["name"].Value);
                    }
                    if (node.Attributes["tests"].Value != "0")
                    {
                        foundTests = true;
                    }
                }

                if (failures.Count > 0)
                {
                    throw new Exception(string.Join(Environment.NewLine, failures));
                }

                if (!foundTests)
                {
                    logger.LogDebug(results);
                    throw new InvalidOperationException("No test case results found.");
                }
            }
        }
    }
}