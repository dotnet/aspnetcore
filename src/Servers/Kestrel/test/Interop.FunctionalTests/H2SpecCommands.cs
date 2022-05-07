// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Interop.FunctionalTests;

public static partial class H2SpecCommands
{
    #region chmod
    // user permissions
    const int S_IRUSR = 0x100;
    const int S_IWUSR = 0x80;
    const int S_IXUSR = 0x40;

    // group permission
    const int S_IRGRP = 0x20;
    const int S_IXGRP = 0x8;

    // other permissions
    const int S_IROTH = 0x4;
    const int S_IXOTH = 0x1;

    const int _0755 =
        S_IRUSR | S_IXUSR | S_IWUSR
        | S_IRGRP | S_IXGRP
        | S_IROTH | S_IXOTH;

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int chmod(string pathname, int mode);

    private static int chmod755(string pathname) => chmod(pathname, _0755);
    #endregion

    private const int TimeoutSeconds = 15;

    private static string GetToolLocation()
    {
        if (RuntimeInformation.OSArchitecture != Architecture.X64)
        {
            // This is a known, unsupported scenario, no-op.
            return null;
        }

        var root = Path.Combine(Environment.CurrentDirectory, "h2spec");
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(root, "windows", "h2spec.exe");
        }
        else if (OperatingSystem.IsLinux())
        {
            var toolPath = Path.Combine(root, "linux", "h2spec");
            chmod755(toolPath);
            return toolPath;
        }
        else if (OperatingSystem.IsMacOS())
        {
            var toolPath = Path.Combine(root, "darwin", "h2spec");
            chmod755(toolPath);
            return toolPath;
        }
        throw new NotImplementedException("Invalid OS");
    }

    public static IList<Tuple<string, string>> EnumerateTestCases()
    {
        // The tool isn't supported on some platforms (arm64), so we can't even enumerate the tests.
        var toolLocation = GetToolLocation();
        if (toolLocation == null)
        {
            return null;
        }

        var testCases = new List<Tuple<string, string>>();
        var processOptions = new ProcessStartInfo
        {
            FileName = toolLocation,
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
        if (line.StartsWith(" ", StringComparison.Ordinal))
        {
            groupName = null;
            return false;
        }

        if (line.StartsWith("Hypertext", StringComparison.Ordinal))
        {
            groupName = "http2";
            return true;
        }
        if (line.StartsWith("Generic", StringComparison.Ordinal))
        {
            groupName = "generic";
            return true;
        }
        if (line.StartsWith("HPACK", StringComparison.Ordinal))
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
        var firstSpace = line.IndexOf(" ", StringComparison.Ordinal);
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
        var firstSpace = line.IndexOf(" ", StringComparison.Ordinal);
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

    public static async Task RunTest(string testId, int port, bool https, ILogger logger)
    {
        var tempFile = Path.GetTempPath() + Guid.NewGuid() + ".xml";
        using (var process = new Process())
        {
            process.StartInfo.FileName = GetToolLocation();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.Arguments = $"{testId} -p {port.ToString(CultureInfo.InvariantCulture)} --strict -v -j {tempFile} --timeout {TimeoutSeconds}"
                + (https ? " --tls --insecure" : "");
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    logger.LogDebug(args.Data);
                }
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    logger.LogError(args.Data);
                }
            };
            var exitedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            process.EnableRaisingEvents = true; // Enables Exited
            process.Exited += (_, args) =>
            {
                logger.LogDebug("H2spec has exited.");
                exitedTcs.TrySetResult();
            };

            Assert.True(process.Start());
            process.BeginOutputReadLine(); // Starts OutputDataReceived
            process.BeginErrorReadLine(); // Starts ErrorDataReceived

            if (await Task.WhenAny(exitedTcs.Task, Task.Delay(TimeSpan.FromSeconds(TimeoutSeconds * 2))) != exitedTcs.Task)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    throw new TimeoutException($"h2spec didn't exit within {TimeoutSeconds * 2} seconds.", ex);
                }
                throw new TimeoutException($"h2spec didn't exit within {TimeoutSeconds * 2} seconds.");
            }

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
                    failures.Add("Test failed: " + node.Attributes["package"].Value + "; " + node.Attributes["name"].Value);
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
