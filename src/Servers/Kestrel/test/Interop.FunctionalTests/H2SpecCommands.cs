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
    private const int TimeoutSeconds = 30;

    private static string GetToolLocation()
    {
        if (RuntimeInformation.OSArchitecture != Architecture.X64)
        {
            // This is a known, unsupported scenario, no-op.
            return null;
        }

        var root = Path.Combine(Environment.CurrentDirectory, "h2spec");
        string toolPath;

        if (OperatingSystem.IsWindows())
        {
            toolPath = Path.Combine(root, "windows", "h2spec.exe");
        }
        else if (OperatingSystem.IsLinux())
        {
            toolPath = Path.Combine(root, "linux", "h2spec");
        }
        else if (OperatingSystem.IsMacOS())
        {
            toolPath = Path.Combine(root, "darwin", "h2spec");
        }
        else
        {
            throw new NotImplementedException("Invalid OS");
        }

        if (!OperatingSystem.IsWindows())
        {
            // 0755: Owner (RWX), Group (RX), Other (RX)
            File.SetUnixFileMode(toolPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        return toolPath;
    }

    public static IList<Tuple<string, string>> EnumerateTestCases()
    {
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
            var groupName = string.Empty;
            var sectionId = string.Empty;

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

    private static bool IsSectionLine(string line, out string section)
    {
        line = line.TrimStart();
        var firstSpace = line.IndexOf(" ", StringComparison.Ordinal);

        if (firstSpace < 2)
        {
            section = string.Empty;
            return false;
        }

        if (line[firstSpace - 1] == '.')
        {
            section = line.Substring(0, firstSpace - 1);
            return true;
        }

        section = string.Empty;
        return false;
    }

    private static bool IsTestLine(string line, out string testNumber, out string description)
    {
        line = line.TrimStart();
        var firstSpace = line.IndexOf(" ", StringComparison.Ordinal);

        if (firstSpace < 2)
        {
            testNumber = string.Empty;
            description = string.Empty;
            return false;
        }

        if (line[firstSpace - 1] == ':')
        {
            testNumber = line.Substring(0, firstSpace - 1);
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
            process.StartInfo.Arguments =
                $"{testId} -p {port.ToString(CultureInfo.InvariantCulture)} --strict -v -j {tempFile} --timeout {TimeoutSeconds}"
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

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                logger.LogDebug("H2spec has exited.");
                exitedTcs.TrySetResult();
            };

            Assert.True(process.Start());

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

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

            var foundTests = false;
            var failures = new List<string>();

            foreach (XmlNode node in xml.GetElementsByTagName("testsuite"))
            {
                if (node.Attributes["errors"].Value != "0")
                {
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
