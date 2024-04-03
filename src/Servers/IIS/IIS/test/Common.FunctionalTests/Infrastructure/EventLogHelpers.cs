// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public class EventLogHelpers
{
    public static void VerifyEventLogEvent(IISDeploymentResult deploymentResult, string expectedRegexMatchString, ILogger logger, bool allowMultiple = false)
    {
        Assert.True(deploymentResult.HostProcess.HasExited);

        var entries = GetEntries(deploymentResult);
        try
        {
            AssertEntry(expectedRegexMatchString, entries, allowMultiple);
        }
        catch (Exception)
        {
            foreach (var entry in entries)
            {
                logger.LogInformation("'{Message}', generated {Generated}, written {Written}", entry.Message, entry.TimeGenerated, entry.TimeWritten);
            }
            throw;
        }
    }

    public static void VerifyEventLogEvents(IISDeploymentResult deploymentResult, params string[] expectedRegexMatchString)
    {
        Assert.True(deploymentResult.HostProcess.HasExited);

        var entries = GetEntries(deploymentResult).ToList();
        foreach (var regexString in expectedRegexMatchString)
        {
            var matchedEntries = AssertEntry(regexString, entries);

            foreach (var matchedEntry in matchedEntries)
            {
                entries.Remove(matchedEntry);
            }
        }

        Assert.True(0 == entries.Count, $"Some entries were not matched by any regex {FormatEntries(entries)}");
    }

    public static string OnlyOneAppPerAppPool()
    {
        if (DeployerSelector.HasNewShim)
        {
            return "Only one in-process application is allowed per IIS application pool. Please assign the application '(.*)' to a different IIS application pool.";

        }
        else
        {
            return "Only one inprocess application is allowed per IIS application pool";
        }
    }

    private static EventLogEntry[] AssertEntry(string regexString, IEnumerable<EventLogEntry> entries, bool allowMultiple = false)
    {
        var expectedRegex = new Regex(regexString, RegexOptions.Singleline);
        var matchedEntries = entries.Where(entry => expectedRegex.IsMatch(entry.Message)).ToArray();
        Assert.True(matchedEntries.Length > 0, $"No entries matched by '{regexString}'");
        Assert.True(allowMultiple || matchedEntries.Length < 2, $"Multiple entries matched by '{regexString}': {FormatEntries(matchedEntries)}");
        return matchedEntries;
    }

    private static string FormatEntries(IEnumerable<EventLogEntry> entries)
    {
        return string.Join(",", entries.Select(e => e.Message));
    }

    internal static IEnumerable<EventLogEntry> GetEntries(IISDeploymentResult deploymentResult)
    {
        var eventLog = new EventLog("Application");

        // Eventlog is already sorted based on time of event in ascending time.
        // Check results in reverse order.
        var processIdString = $"Process Id: {deploymentResult.HostProcess.Id}.";

        // Event log messages round down to the nearest second, so subtract 5 seconds to make sure we get event logs
        var processStartTime = deploymentResult.HostProcess.StartTime.AddSeconds(-10);
        for (var i = eventLog.Entries.Count - 1; i >= 0; i--)
        {
            var eventLogEntry = eventLog.Entries[i];
            if (eventLogEntry.TimeGenerated < processStartTime)
            {
                // If event logs is older than the process start time, we didn't find a match.
                break;
            }

            if (eventLogEntry.ReplacementStrings == null ||
                eventLogEntry.ReplacementStrings.Length < 3)
            {
                continue;
            }

            // ReplacementStings == EventData collection in EventLog
            // This is unaffected if event providers are not registered correctly
            if (eventLogEntry.Source == AncmVersionToMatch(deploymentResult) &&
                processIdString == eventLogEntry.ReplacementStrings[1])
            {
                yield return eventLogEntry;
            }
        }
    }

    private static string AncmVersionToMatch(IISDeploymentResult deploymentResult)
    {
        return "IIS " +
            (deploymentResult.DeploymentParameters.ServerType == ServerType.IISExpress ? "Express " : "") +
            "AspNetCore Module V2";
    }

    public static string Started(IISDeploymentResult deploymentResult)
    {
        if (deploymentResult.DeploymentParameters.HostingModel == HostingModel.InProcess)
        {
            return InProcessStarted(deploymentResult);
        }
        else
        {
            return OutOfProcessStarted(deploymentResult);
        }
    }

    public static string InProcessStarted(IISDeploymentResult deploymentResult)
    {
        if (DeployerSelector.HasNewHandler)
        {
            return $"Application '{EscapedContentRoot(deploymentResult)}' started successfully.";
        }
        else
        {
            return $"Application '{EscapedContentRoot(deploymentResult)}' started the coreclr in-process successfully";
        }
    }

    public static string OutOfProcessStarted(IISDeploymentResult deploymentResult)
    {
        return $"Application '/LM/W3SVC/1/ROOT' started process '\\d+' successfully and process '\\d+' is listening on port '\\d+'.";
    }

    public static string InProcessFailedToStart(IISDeploymentResult deploymentResult, string reason)
    {
        if (DeployerSelector.HasNewHandler)
        {
            return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' failed to load coreclr. Exception message:\r\n{reason}";
        }
        else
        {
            return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' failed to load clr and managed application. {reason}";
        }
    }

    public static string ShutdownMessage(IISDeploymentResult deploymentResult)
    {
        if (deploymentResult.DeploymentParameters.HostingModel == HostingModel.InProcess)
        {
            return "Application 'MACHINE/WEBROOT/APPHOST/.*?' has shutdown.";
        }
        else
        {
            return "Application '/LM/W3SVC/1/ROOT' with physical root '.*?' shut down process with Id '.*?' listening on port '.*?'";
        }
    }

    public static string ShutdownFileChange(IISDeploymentResult deploymentResult)
    {
        return $"Application '{EscapedContentRoot(deploymentResult)}' was recycled after detecting file change in application directory.";
    }

    public static string InProcessFailedToStop(IISDeploymentResult deploymentResult, string reason)
    {
        return "Failed to gracefully shutdown application 'MACHINE/WEBROOT/APPHOST/.*?'.";
    }

    public static string InProcessThreadException(IISDeploymentResult deploymentResult, string reason)
    {
        return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' hit unexpected managed exception{reason}";
    }

    public static string InProcessThreadExit(IISDeploymentResult deploymentResult, string code)
    {
        if (DeployerSelector.HasNewHandler)
        {
            return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' has exited from Program.Main with exit code = '{code}'. Please check the stderr logs for more information.";
        }
        else
        {
            return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' hit unexpected managed background thread exit, exit code = '{code}'.";
        }
    }
    public static string InProcessThreadExitStdOut(IISDeploymentResult deploymentResult, string code, string output)
    {
        if (DeployerSelector.HasNewHandler)
        {
            return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' has exited from Program.Main with exit code = '{code}'. First 30KB characters of captured stdout and stderr logs:\r\n{output}";
        }
        else
        {
            return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' hit unexpected managed background thread exit, exit code = '{code}'. Last 4KB characters of captured stdout and stderr logs:\r\n{output}";
        }
    }

    public static string FailedToStartApplication(IISDeploymentResult deploymentResult, string code)
    {
        return $"Failed to start application '/LM/W3SVC/1/ROOT', ErrorCode '{code}'.";
    }

    public static string ConfigurationLoadError(IISDeploymentResult deploymentResult, string reason)
    {
        if (DeployerSelector.HasNewShim)
        {
            return $"Could not load configuration. Exception message:\r\n{reason}";
        }
        else
        {
            return $"Could not load configuration. Exception message: {reason}";
        }
    }

    public static string OutOfProcessFailedToStart(IISDeploymentResult deploymentResult, string output)
    {
        if (DeployerSelector.HasNewShim)
        {
            return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' failed to start process with " +
                $"commandline '(.*)' with multiple retries. " +
                $"Failed to bind to port '(.*)'. First 30KB characters of captured stdout and stderr logs from multiple retries:\r\n{output}";
        }
        else
        {
            return $"Application '/LM/W3SVC/1/ROOT' with physical root '{EscapedContentRoot(deploymentResult)}' failed to start process with " +
                $"commandline '(.*)' with multiple retries. " +
                $"The last try of listening port is '(.*)'. See previous warnings for details.";
        }
    }

    public static string InProcessHostfxrInvalid(IISDeploymentResult deploymentResult)
    {
        return $"Hostfxr version used does not support 'hostfxr_get_native_search_directories', update the version of hostfxr to a higher version. Path to hostfxr: '(.*)'.";
    }

    public static string InProcessHostfxrUnableToLoad(IISDeploymentResult deploymentResult)
    {
        return $"Unable to load '(.*)'. This might be caused by a bitness mismatch between IIS application pool and published application.";
    }

    public static string InProcessFailedToFindNativeDependencies(IISDeploymentResult deploymentResult)
    {
        if (DeployerSelector.HasNewShim)
        {
            return "Unable to locate application dependencies. Ensure that the versions of Microsoft.NetCore.App and Microsoft.AspNetCore.App targeted by the application are installed.";
        }
        else
        {
            return "Invoking hostfxr to find the inprocess request handler failed without finding any native dependencies. " +
                "This most likely means the app is misconfigured, please check the versions of Microsoft.NetCore.App and Microsoft.AspNetCore.App that " +
                "are targeted by the application and are installed on the machine.";
        }
    }

    public static string InProcessFailedToFindApplication()
    {
        return "Provided application path does not exist, or isn't a .dll or .exe.";
    }

    public static string InProcessFailedToFindRequestHandler(IISDeploymentResult deploymentResult)
    {
        if (DeployerSelector.HasNewShim)
        {
            return "Could not find the assembly '(.*)' referenced for the in-process application. Please confirm the Microsoft.AspNetCore.Server.IIS or Microsoft.AspNetCore.App is referenced in your application.";

        }
        else
        {
            return "Could not find the assembly '(.*)' referenced for the in-process application. Please confirm the Microsoft.AspNetCore.Server.IIS package is referenced in your application.";
        }
    }

    public static string CouldNotStartStdoutFileRedirection(string file, IISDeploymentResult deploymentResult)
    {
        return
            $"Could not start stdout file redirection to '{Regex.Escape(file)}' with application base '{EscapedContentRoot(deploymentResult)}'.";
    }

    public static string CouldNotFindHandler()
    {
        if (DeployerSelector.HasNewShim)
        {
            return "Could not find 'aspnetcorev2_inprocess.dll'";
        }
        else
        {
            return "Could not find the assembly 'aspnetcorev2_inprocess.dll'";
        }
    }

    public static string UnableToStart(IISDeploymentResult deploymentResult, string subError)
    {
        if (DeployerSelector.HasNewShim)
        {
            return $@"Application '{Regex.Escape(deploymentResult.ContentRoot)}\\' failed to start. Exception message:\r\n{subError}";
        }
        else
        {
            return $@"Application '{Regex.Escape(deploymentResult.ContentRoot)}\\' wasn't able to start. {subError}";
        }
    }

    public static string FrameworkNotFound()
    {
        if (DeployerSelector.HasNewShim)
        {
            return "Unable to locate application dependencies. Ensure that the versions of Microsoft.NetCore.App and Microsoft.AspNetCore.App targeted by the application are installed.";
        }
        else
        {
            return "The framework 'Microsoft.NETCore.App', version '2.9.9' was not found.";
        }
    }

    private static string EscapedContentRoot(IISDeploymentResult deploymentResult)
    {
        var contentRoot = deploymentResult.ContentRoot;
        if (!contentRoot.EndsWith('\\'))
        {
            contentRoot += '\\';
        }
        return Regex.Escape(contentRoot);
    }
}
